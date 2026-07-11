using FluentValidation;
using MediatR;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Shared.Common;

namespace RestaurantPOS.Application.Features.Orders.Commands;

// ============================================================
// DTOs
// ============================================================

public record OrderItemRequest(
    Guid ProductId,
    Guid? VariantId,
    decimal Quantity,
    string? Notes,
    List<OrderItemModifierRequest>? Modifiers);

public record OrderItemModifierRequest(Guid ModifierId, decimal Quantity = 1);

// ============================================================
// Create Order Command
// ============================================================

/// <summary>Creates a new POS order.</summary>
public record CreateOrderCommand(
    Guid BranchId,
    OrderType OrderType,
    Guid? TableId,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    string? Notes,
    int NumberOfGuests,
    List<OrderItemRequest> Items,
    Guid? CouponId,
    string? CouponCode,
    Guid? DiscountId
) : IRequest<Result<Guid>>;

/// <summary>Handler for creating a new order.</summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    private readonly Domain.Interfaces.ICurrentUserService _currentUser;
    private readonly Domain.Interfaces.INumberGenerator _numberGenerator;
    private readonly Domain.Interfaces.INotificationService _notifications;

    public CreateOrderCommandHandler(
        Domain.Interfaces.IUnitOfWork unitOfWork,
        Domain.Interfaces.ICurrentUserService currentUser,
        Domain.Interfaces.INumberGenerator numberGenerator,
        Domain.Interfaces.INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _numberGenerator = numberGenerator;
        _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var orderNumber = await _numberGenerator.GenerateOrderNumberAsync(request.BranchId, cancellationToken);

            var order = new Domain.Entities.Order
            {
                BranchId = request.BranchId,
                CashierId = _currentUser.UserId,
                CustomerId = request.CustomerId,
                TableId = request.TableId,
                OrderNumber = orderNumber,
                OrderType = request.OrderType,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                Notes = request.Notes,
                NumberOfGuests = request.NumberOfGuests
            };

            // Build order items
            var productRepo = _unitOfWork.Repository<Domain.Entities.Product>();
            decimal subTotal = 0;
            decimal taxTotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = await productRepo.GetByIdAsync(itemRequest.ProductId, cancellationToken);
                if (product is null)
                    return Result<Guid>.Failure($"Product {itemRequest.ProductId} not found.");

                var unitPrice = product.SellingPrice;
                decimal modifierTotal = 0;

                var orderItem = new Domain.Entities.OrderItem
                {
                    ProductId = product.Id,
                    VariantId = itemRequest.VariantId,
                    ProductName = product.Name,
                    ProductNameAr = product.NameAr,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = unitPrice,
                    CostPrice = product.CostPrice,
                    Notes = itemRequest.Notes,
                    KitchenStatus = KitchenOrderStatus.New
                };

                // Modifiers
                if (itemRequest.Modifiers?.Any() == true)
                {
                    var modifierRepo = _unitOfWork.Repository<Domain.Entities.Modifier>();
                    foreach (var mod in itemRequest.Modifiers)
                    {
                        var modifier = await modifierRepo.GetByIdAsync(mod.ModifierId, cancellationToken);
                        if (modifier is not null)
                        {
                            orderItem.Modifiers.Add(new Domain.Entities.OrderItemModifier
                            {
                                ModifierId = modifier.Id,
                                ModifierName = modifier.Name,
                                Price = modifier.Price,
                                Quantity = mod.Quantity
                            });
                            modifierTotal += modifier.Price * mod.Quantity;
                        }
                    }
                }

                var lineTotal = (unitPrice + modifierTotal) * itemRequest.Quantity;
                var taxRate = product.TaxRate?.Rate ?? 0;
                var taxAmount = lineTotal * (taxRate / 100);

                orderItem.TaxRate = taxRate;
                orderItem.TaxAmount = taxAmount;
                orderItem.TotalPrice = lineTotal + taxAmount;

                subTotal += lineTotal;
                taxTotal += taxAmount;
                order.Items.Add(orderItem);
            }

            // Apply discounts
            decimal discountAmount = 0;
            if (request.CouponCode is not null)
            {
                var couponRepo = _unitOfWork.Repository<Domain.Entities.Coupon>();
                var coupon = await couponRepo.FirstOrDefaultAsync(
                    c => c.Code == request.CouponCode && c.IsActive, cancellationToken);
                if (coupon is not null)
                {
                    discountAmount = coupon.DiscountType == DiscountType.Percentage
                        ? subTotal * (coupon.Value / 100)
                        : coupon.Value;
                    if (coupon.MaxDiscountAmount.HasValue)
                        discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
                    order.CouponId = coupon.Id;
                    order.CouponCode = coupon.Code;
                }
            }

            order.SubTotal = subTotal;
            order.DiscountAmount = discountAmount;
            order.TaxAmount = taxTotal;
            order.TotalAmount = subTotal - discountAmount + taxTotal;

            var shiftRepo = _unitOfWork.Repository<Domain.Entities.Shift>();
            var shift = await shiftRepo.FirstOrDefaultAsync(
                s => s.BranchId == request.BranchId && s.Status == ShiftStatus.Open
                     && s.CashierId == _currentUser.UserId, cancellationToken);
            if (shift is not null) order.ShiftId = shift.Id;

            await _unitOfWork.Repository<Domain.Entities.Order>().AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Update table status
            if (request.TableId.HasValue && request.OrderType == OrderType.DineIn)
            {
                var tableRepo = _unitOfWork.Repository<Domain.Entities.DiningTable>();
                var table = await tableRepo.GetByIdAsync(request.TableId.Value, cancellationToken);
                if (table is not null)
                {
                    table.Status = TableStatus.Occupied;
                    tableRepo.Update(table);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Create kitchen order
            var kitchenOrder = new Domain.Entities.KitchenOrder
            {
                OrderId = order.Id,
                BranchId = request.BranchId,
                OrderNumber = order.OrderNumber,
                Status = KitchenOrderStatus.New
            };
            await _unitOfWork.Repository<Domain.Entities.KitchenOrder>().AddAsync(kitchenOrder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Notify kitchen via SignalR
            await _notifications.SendToBranchAsync(
                request.BranchId.ToString(),
                "NewOrder",
                new { OrderId = order.Id, OrderNumber = order.OrderNumber, OrderType = order.OrderType.ToString() },
                cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>FluentValidation rules for CreateOrderCommand.</summary>
public class CreateOrderCommandValidator : FluentValidation.AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Branch is required.");
        RuleFor(x => x.OrderType).IsInEnum().WithMessage("Invalid order type.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        });
        When(x => x.OrderType == OrderType.Delivery, () =>
        {
            RuleFor(x => x.CustomerAddress).NotEmpty().WithMessage("Delivery address is required for delivery orders.");
        });
    }
}

// ============================================================
// Cancel Order Command
// ============================================================

/// <summary>Cancels an existing order.</summary>
public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    private readonly Domain.Interfaces.ICurrentUserService _currentUser;

    public CancelOrderCommandHandler(
        Domain.Interfaces.IUnitOfWork unitOfWork,
        Domain.Interfaces.ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Domain.Entities.Order>();
        var order = await repo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null) return Result.Failure("Order not found.", "ORDER_NOT_FOUND");

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            return Result.Failure("Cannot cancel a completed or already cancelled order.", "INVALID_STATUS");

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = request.Reason;
        order.CancelledAt = DateTime.UtcNow;
        repo.Update(order);

        // Release table if dine-in
        if (order.TableId.HasValue)
        {
            var tableRepo = _unitOfWork.Repository<Domain.Entities.DiningTable>();
            var table = await tableRepo.GetByIdAsync(order.TableId.Value, cancellationToken);
            if (table is not null)
            {
                table.Status = TableStatus.Available;
                tableRepo.Update(table);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ============================================================
// Process Payment Command
// ============================================================

public record PaymentRequest(PaymentMethod Method, decimal Amount, string? Reference = null, string? CardLast4 = null);

/// <summary>Processes payment(s) for an order and marks it as paid.</summary>
public record ProcessPaymentCommand(Guid OrderId, List<PaymentRequest> Payments) : IRequest<Result<Guid>>;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<Guid>>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    private readonly Domain.Interfaces.INumberGenerator _numberGenerator;

    public ProcessPaymentCommandHandler(
        Domain.Interfaces.IUnitOfWork unitOfWork,
        Domain.Interfaces.INumberGenerator numberGenerator)
    {
        _unitOfWork = unitOfWork;
        _numberGenerator = numberGenerator;
    }

    public async Task<Result<Guid>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var orderRepo = _unitOfWork.Repository<Domain.Entities.Order>();
            var order = await orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
            if (order is null) return Result<Guid>.Failure("Order not found.");
            if (order.PaymentStatus == PaymentStatus.Paid)
                return Result<Guid>.Failure("Order is already paid.");

            var totalPaid = request.Payments.Sum(p => p.Amount);
            if (totalPaid < order.TotalAmount)
                return Result<Guid>.Failure($"Insufficient payment. Required: {order.TotalAmount:F2}, Paid: {totalPaid:F2}");

            var paymentRepo = _unitOfWork.Repository<Domain.Entities.Payment>();
            foreach (var p in request.Payments)
            {
                await paymentRepo.AddAsync(new Domain.Entities.Payment
                {
                    OrderId = order.Id,
                    Method = p.Method,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    CardLast4 = p.CardLast4
                }, cancellationToken);
            }

            order.PaidAmount = totalPaid;
            order.ChangeAmount = totalPaid - order.TotalAmount;
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            orderRepo.Update(order);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate invoice
            var invoiceNumber = await _numberGenerator.GenerateInvoiceNumberAsync(order.BranchId, cancellationToken);
            var invoice = new Domain.Entities.Invoice
            {
                OrderId = order.Id,
                BranchId = order.BranchId,
                CustomerId = order.CustomerId,
                InvoiceNumber = invoiceNumber,
                Uuid = Guid.NewGuid().ToString(),
                InvoiceDate = DateTime.UtcNow,
                CustomerName = order.CustomerName,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TaxableAmount = order.SubTotal - order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                ZatcaStatus = ZatcaStatus.Pending
            };

            // Copy items to invoice snapshot
            foreach (var item in order.Items.Where(i => !i.IsVoid))
            {
                invoice.Items.Add(new Domain.Entities.InvoiceItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductNameAr = item.ProductNameAr,
                    VariantName = item.VariantName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    TotalPrice = item.TotalPrice
                });
            }

            await _unitOfWork.Repository<Domain.Entities.Invoice>().AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Free up the table
            if (order.TableId.HasValue)
            {
                var tableRepo = _unitOfWork.Repository<Domain.Entities.DiningTable>();
                var table = await tableRepo.GetByIdAsync(order.TableId.Value, cancellationToken);
                if (table is not null)
                {
                    table.Status = TableStatus.Available;
                    tableRepo.Update(table);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<Guid>.Success(invoice.Id);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
