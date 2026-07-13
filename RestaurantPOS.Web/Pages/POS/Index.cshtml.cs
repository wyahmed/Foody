using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Application.Features.Orders.Commands;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.POS;

/// <summary>POS screen page model – loads products, categories, and tables for the current branch.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public IndexModel(ApplicationDbContext context, ICurrentUserService currentUser, IMediator mediator)
    {
        _context = context;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public List<PosProductDto> Products { get; private set; } = new();
    public List<PosCategoryDto> Categories { get; private set; } = new();
    public List<PosTableDto> Tables { get; private set; } = new();
    public string SelectedOrderType { get; private set; } = "DineIn";
    public string? BranchId => _currentUser.BranchId?.ToString();

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
            return new JsonResult(new { error = "Order must have at least one item." }) { StatusCode = StatusCodes.Status400BadRequest };

        var branchId = _currentUser.BranchId;
        if (branchId is null)
            return new JsonResult(new { error = "Branch is required." }) { StatusCode = StatusCodes.Status400BadRequest };

        if (!Enum.TryParse<OrderType>(request.OrderType, true, out var orderType))
            return new JsonResult(new { error = "Invalid order type." }) { StatusCode = StatusCodes.Status400BadRequest };

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            return new JsonResult(new { error = "Invalid payment method." }) { StatusCode = StatusCodes.Status400BadRequest };

        var createCommand = new CreateOrderCommand(
            branchId.Value,
            orderType,
            request.TableId,
            null,
            null,
            null,
            null,
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

        var paymentAmount = request.TenderedAmount <= 0 ? 0 : request.TenderedAmount;
        var paymentResult = await _mediator.Send(
            new ProcessPaymentCommand(createResult.Value, [new PaymentRequest(paymentMethod, paymentAmount)]),
            cancellationToken);

        if (paymentResult.IsFailure)
            return new JsonResult(new { error = paymentResult.Error }) { StatusCode = StatusCodes.Status400BadRequest };

        return new JsonResult(new { orderId = createResult.Value, invoiceId = paymentResult.Value });
    }

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
        decimal TenderedAmount);

    public record CheckoutItemRequest(Guid ProductId, decimal Quantity, string? Notes, List<CheckoutItemModifierRequest>? Modifiers);
    public record CheckoutItemModifierRequest(Guid ModifierId);
}
