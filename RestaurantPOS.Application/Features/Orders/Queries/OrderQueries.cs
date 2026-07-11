using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Application.Common.Exceptions;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Shared.Models;

namespace RestaurantPOS.Application.Features.Orders.Queries;

// ============================================================
// DTOs
// ============================================================

public record OrderListDto(
    Guid Id,
    string OrderNumber,
    OrderType OrderType,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string? CustomerName,
    string? TableName,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedDate,
    string? CashierName);

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderType OrderType,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    string? Notes,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal DeliveryChargeAmount,
    decimal TipsAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount,
    string? CouponCode,
    DateTime CreatedDate,
    DateTime? CompletedAt,
    string? CashierName,
    string? TableName,
    List<OrderItemDto> Items,
    List<PaymentDto> Payments);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductNameAr,
    string? VariantName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalPrice,
    string? Notes,
    bool IsVoid,
    KitchenOrderStatus KitchenStatus,
    List<OrderItemModifierDto> Modifiers);

public record OrderItemModifierDto(Guid ModifierId, string ModifierName, decimal Price, decimal Quantity);

public record PaymentDto(Guid Id, PaymentMethod Method, decimal Amount, decimal ChangeAmount, string? Reference, DateTime PaymentDate);

// ============================================================
// Get Order by ID
// ============================================================

/// <summary>Returns full order detail by ID.</summary>
public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public GetOrderByIdQueryHandler(Domain.Interfaces.IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<Domain.Entities.Order>()
            .Query()
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null) throw new NotFoundException(nameof(Domain.Entities.Order), request.OrderId);

        return MapToDetail(order);
    }

    private static OrderDetailDto MapToDetail(Domain.Entities.Order o) => new(
        o.Id, o.OrderNumber, o.OrderType, o.Status, o.PaymentStatus,
        o.CustomerName, o.CustomerPhone, o.CustomerAddress, o.Notes,
        o.SubTotal, o.DiscountAmount, o.TaxAmount, o.ServiceChargeAmount,
        o.DeliveryChargeAmount, o.TipsAmount, o.TotalAmount, o.PaidAmount, o.ChangeAmount,
        o.CouponCode, o.CreatedDate, o.CompletedAt,
        null, o.Table?.Name,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.ProductName, i.ProductNameAr, i.VariantName,
            i.Quantity, i.UnitPrice, i.TaxRate, i.TaxAmount, i.TotalPrice,
            i.Notes, i.IsVoid, i.KitchenStatus,
            i.Modifiers.Select(m => new OrderItemModifierDto(m.ModifierId, m.ModifierName, m.Price, m.Quantity)).ToList()
        )).ToList(),
        o.Payments.Select(p => new PaymentDto(p.Id, p.Method, p.Amount, p.ChangeAmount, p.Reference, p.PaymentDate)).ToList()
    );
}

// ============================================================
// Get Orders (Paged)
// ============================================================

/// <summary>Returns paged list of orders for a branch, with optional filters.</summary>
public record GetOrdersQuery(
    Guid BranchId,
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    OrderType? OrderType = null,
    DateTime? From = null,
    DateTime? To = null,
    string? Search = null
) : IRequest<PagedResult<OrderListDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderListDto>>
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public GetOrdersQueryHandler(Domain.Interfaces.IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<PagedResult<OrderListDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Domain.Entities.Order>()
            .Query()
            .Include(o => o.Table)
            .Include(o => o.Items)
            .Where(o => o.BranchId == request.BranchId);

        if (request.Status.HasValue) query = query.Where(o => o.Status == request.Status.Value);
        if (request.OrderType.HasValue) query = query.Where(o => o.OrderType == request.OrderType.Value);
        if (request.From.HasValue) query = query.Where(o => o.CreatedDate >= request.From.Value);
        if (request.To.HasValue) query = query.Where(o => o.CreatedDate <= request.To.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(o => o.OrderNumber.Contains(request.Search)
                || (o.CustomerName != null && o.CustomerName.Contains(request.Search)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderListDto(
                o.Id, o.OrderNumber, o.OrderType, o.Status, o.PaymentStatus,
                o.CustomerName, o.Table != null ? o.Table.Name : null,
                o.TotalAmount, o.Items.Count(i => !i.IsVoid),
                o.CreatedDate, null))
            .ToListAsync(cancellationToken);

        return PagedResult<OrderListDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}
