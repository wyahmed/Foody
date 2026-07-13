using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantPOS.Application.Common.Exceptions;
using RestaurantPOS.Application.Features.Orders.Commands;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using System.Globalization;

namespace RestaurantPOS.Web.Pages.POS;

/// <summary>POS screen page model – loads products, categories, and tables for the current branch.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ICurrentUserService currentUser, IMediator mediator, ILogger<IndexModel> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _mediator = mediator;
        _logger = logger;
    }

    public List<PosProductDto> Products { get; private set; } = new();
    public List<PosCategoryDto> Categories { get; private set; } = new();
    public List<PosTableDto> Tables { get; private set; } = new();
    public string SelectedOrderType { get; private set; } = "DineIn";
    public string? BranchId => _currentUser.BranchId?.ToString();
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        if (branchId is null) return;

        // Load active categories
        Categories = await _context.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new PosCategoryDto(c.Id, c.Name, c.NameAr, c.Color, c.Icon))
            .ToListAsync();

        // Load active products for this branch
        var branchProductIds = await _context.BranchProducts
            .Where(bp => bp.BranchId == branchId && bp.IsActive && !bp.IsOutOfStock)
            .Select(bp => bp.ProductId)
            .ToListAsync();

        Products = await _context.Products
            .Include(p => p.TaxRate)
            .Where(p => p.TenantId == tenantId && p.IsActive
                        && (branchProductIds.Count == 0 || branchProductIds.Contains(p.Id)))
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new PosProductDto(
                p.Id,
                p.Name,
                p.NameAr,
                p.SellingPrice,
                p.CostPrice,
                p.Barcode,
                p.ImageUrl,
                p.CategoryId,
                p.TaxRate != null ? p.TaxRate.Rate : 0,
                p.IsWeightBased,
                p.IsOpenPrice))
            .ToListAsync();

        // Load available tables
        Tables = await _context.DiningTables
            .Where(t => t.BranchId == branchId && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new PosTableDto(t.Id, t.Name, t.Status.ToString(), t.Capacity))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCheckoutAsync([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequestJson("Order must have at least one item.", "يجب أن يحتوي الطلب على صنف واحد على الأقل.");

        var branchId = _currentUser.BranchId;
        if (branchId is null)
            return BadRequestJson("Branch is required.", "الفرع مطلوب.");

        if (!Enum.TryParse<OrderType>(request.OrderType, true, out var orderType))
            return BadRequestJson("Invalid order type.", "نوع الطلب غير صالح.");

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            return BadRequestJson("Invalid payment method.", "طريقة الدفع غير صالحة.");

        if (orderType == OrderType.Delivery && string.IsNullOrWhiteSpace(request.DeliveryAddress))
            return BadRequestJson("Delivery address is required for delivery orders.", "عنوان التوصيل مطلوب لطلبات التوصيل.");

        try
        {
            var createCommand = new CreateOrderCommand(
                branchId.Value,
                orderType,
                request.TableId,
                null,
                null,
                null,
                string.IsNullOrWhiteSpace(request.DeliveryAddress) ? null : request.DeliveryAddress.Trim(),
                request.Notes,
                request.NumberOfGuests <= 0 ? 1 : request.NumberOfGuests,
                request.Items.Select(i => new OrderItemRequest(
                    i.ProductId,
                    null,
                    i.Quantity <= 0 ? 1 : i.Quantity,
                    i.Notes,
                    i.Modifiers?.Select(m => new OrderItemModifierRequest(m.ModifierId, 1)).ToList())).ToList(),
                null,
                request.CouponCode,
                null);

            var createResult = await _mediator.Send(createCommand, cancellationToken);
            if (createResult.IsFailure)
                return new JsonResult(new { error = createResult.Error }) { StatusCode = StatusCodes.Status400BadRequest };

            var paymentAmount = request.TenderedAmount;
            if (paymentAmount <= 0)
            {
                if (paymentMethod == PaymentMethod.Cash)
                    return BadRequestJson("Paid amount is required.", "المبلغ المدفوع مطلوب.");

                paymentAmount = await _context.Orders
                    .Where(o => o.Id == createResult.Value)
                    .Select(o => o.TotalAmount)
                    .FirstAsync(cancellationToken);
            }

            var paymentResult = await _mediator.Send(
                new ProcessPaymentCommand(createResult.Value, [new PaymentRequest(paymentMethod, paymentAmount)]),
                cancellationToken);

            if (paymentResult.IsFailure)
                return new JsonResult(new { error = paymentResult.Error }) { StatusCode = StatusCodes.Status400BadRequest };

            return new JsonResult(new { orderId = createResult.Value, invoiceId = paymentResult.Value });
        }
        catch (ValidationException ex)
        {
            var errorMessage = ex.Errors.SelectMany(e => e.Value).FirstOrDefault()
                ?? (IsArabic ? "تعذر التحقق من الطلب." : "Order validation failed.");
            return new JsonResult(new { error = errorMessage }) { StatusCode = StatusCodes.Status400BadRequest };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete POS checkout for branch {BranchId}", branchId);
            return new JsonResult(new
            {
                error = IsArabic
                    ? "حدث خطأ غير متوقع أثناء حفظ الطلب."
                    : "An unexpected error occurred while saving the order."
            })
            { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }

    private JsonResult BadRequestJson(string english, string arabic)
        => new(new { error = IsArabic ? arabic : english }) { StatusCode = StatusCodes.Status400BadRequest };

    public record PosProductDto(
        Guid Id, string Name, string NameAr, decimal SellingPrice, decimal CostPrice,
        string? Barcode, string? ImageUrl, Guid? CategoryId, decimal TaxRate,
        bool IsWeightBased, bool IsOpenPrice);

    public record PosCategoryDto(Guid Id, string Name, string NameAr, string? Color, string? Icon);
    public record PosTableDto(Guid Id, string Name, string Status, int Capacity);

    public record CheckoutRequest(
        string OrderType,
        Guid? TableId,
        string? Notes,
        int NumberOfGuests,
        List<CheckoutItemRequest> Items,
        string? CouponCode,
        string PaymentMethod,
        decimal TenderedAmount,
        string? DeliveryAddress);

    public record CheckoutItemRequest(Guid ProductId, decimal Quantity, string? Notes, List<CheckoutItemModifierRequest>? Modifiers);
    public record CheckoutItemModifierRequest(Guid ModifierId);
}
