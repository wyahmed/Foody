using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Web.Extensions;

namespace RestaurantPOS.Web.Pages.Categories;

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
    [BindProperty(SupportsGet = true)] public bool? IsActive { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ParentCategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public List<CategoryRowDto> Categories { get; private set; } = new();
    public List<ParentCategoryOptionDto> ParentCategories { get; private set; } = new();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public int From { get; private set; }
    public int To { get; private set; }

    private const int PageSize = 25;

    public async Task OnGetAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        ParentCategories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ParentCategoryId == null)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new ParentCategoryOptionDto(c.Id, c.Name))
            .ToListAsync();

        var query = _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(c =>
                c.Name.Contains(Search) ||
                c.NameAr.Contains(Search) ||
                (c.Description != null && c.Description.Contains(Search)) ||
                (c.DescriptionAr != null && c.DescriptionAr.Contains(Search)));
        }

        if (IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == IsActive.Value);
        }

        if (ParentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == ParentCategoryId.Value);
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));
        From = TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        To = Math.Min(Page * PageSize, TotalCount);

        Categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CategoryRowDto(
                c.Id,
                c.Name,
                c.NameAr,
                c.ParentCategory != null ? c.ParentCategory.Name : null,
                c.Description,
                c.Color,
                c.Icon,
                c.SortOrder,
                c.IsActive,
                c.Products.Count(),
                c.SubCategories.Count()))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        var category = await _db.Categories
            .Where(c => c.Id == id && c.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (category == null)
        {
            this.SetErrorMessage("Category not found.");
            return RedirectToPage();
        }

        var hasSubCategories = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id && c.TenantId == tenantId);
        if (hasSubCategories)
        {
            this.SetErrorMessage("Delete or reassign subcategories before deleting this category.");
            return RedirectToPage();
        }

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id && p.TenantId == tenantId);
        if (hasProducts)
        {
            this.SetErrorMessage("Delete or reassign products before deleting this category.");
            return RedirectToPage();
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        this.SetSuccessMessage("Category deleted.");
        return RedirectToPage();
    }
}

public record CategoryRowDto(
    Guid Id,
    string Name,
    string NameAr,
    string? ParentCategoryName,
    string? Description,
    string? Color,
    string? Icon,
    int SortOrder,
    bool IsActive,
    int ProductCount,
    int SubCategoryCount);

public record ParentCategoryOptionDto(Guid Id, string Name);
