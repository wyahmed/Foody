using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    public IndexModel(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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

    public record PosProductDto(
        Guid Id, string Name, string NameAr, decimal SellingPrice, decimal CostPrice,
        string? Barcode, string? ImageUrl, Guid? CategoryId, decimal TaxRate,
        bool IsWeightBased, bool IsOpenPrice);

    public record PosCategoryDto(Guid Id, string Name, string NameAr, string? Color, string? Icon);
    public record PosTableDto(Guid Id, string Name, string Status, int Capacity);
}
