using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Products;

[Authorize(Roles = "SuperAdmin,Admin,Manager,InventoryManager")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsActive { get; set; }
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public List<ProductRowDto> Products { get; set; } = new();
    public List<CategoryOptionDto> Categories { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    private const int PageSize = 25;

    public async Task OnGetAsync()
    {
        var tenantId = _currentUser.TenantId;

        Categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Select(c => new CategoryOptionDto { Id = c.Id, Name = c.Name })
            .OrderBy(c => c.Name)
            .ToListAsync();

        var q = _db.Products.AsNoTracking().Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(p => p.Name.Contains(Search) || (p.SKU != null && p.SKU.Contains(Search)) || (p.Barcode != null && p.Barcode.Contains(Search)));

        if (CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == CategoryId.Value);

        if (IsActive.HasValue)
            q = q.Where(p => p.IsActive == IsActive.Value);

        TotalCount = await q.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));
        From = (Page - 1) * PageSize + 1;
        To = Math.Min(Page * PageSize, TotalCount);

        Products = await q
            .OrderBy(p => p.Name)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new ProductRowDto
            {
                Id = p.Id,
                Name = p.Name,
                NameAr = p.NameAr,
                CategoryName = p.Category != null ? p.Category.Name : "",
                SKU = p.SKU,
                Barcode = p.Barcode,
                SellingPrice = p.SellingPrice,
                CostPrice = p.CostPrice,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        var product = await _db.Products
            .Where(p => p.Id == id && p.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (product != null) { product.IsDeleted = true; await _db.SaveChangesAsync(); }
        TempData["Success"] = "Product deleted.";
        return RedirectToPage();
    }
}

public record ProductRowDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? NameAr { get; init; }
    public string CategoryName { get; init; } = "";
    public string? SKU { get; init; }
    public string? Barcode { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal CostPrice { get; init; }
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
}

public record CategoryOptionDto { public Guid Id { get; init; } public string Name { get; init; } = ""; }
