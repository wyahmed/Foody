using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Categories;

[Authorize(Roles = "SuperAdmin,Admin,Manager,InventoryManager")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EditModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public CategoryEditDto Category { get; set; } = new();

    public List<ParentCategoryOptionDto> ParentCategories { get; private set; } = new();
    public bool IsNew => Category.Id == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        if (id.HasValue && id.Value != Guid.Empty)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == id.Value && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            Category = new CategoryEditDto
            {
                Id = category.Id,
                ParentCategoryId = category.ParentCategoryId,
                Name = category.Name,
                NameAr = category.NameAr,
                Description = category.Description,
                DescriptionAr = category.DescriptionAr,
                Color = category.Color,
                Icon = category.Icon,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive
            };
        }

        await LoadParentCategoriesAsync(tenantId, Category.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        await ValidateParentCategoryAsync(tenantId);
        await LoadParentCategoriesAsync(tenantId, Category.Id);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Category.Id == Guid.Empty)
        {
            var category = new Category
            {
                TenantId = tenantId,
                ParentCategoryId = Category.ParentCategoryId,
                Name = Category.Name.Trim(),
                NameAr = Category.NameAr?.Trim() ?? string.Empty,
                Description = TrimOrNull(Category.Description),
                DescriptionAr = TrimOrNull(Category.DescriptionAr),
                Color = TrimOrNull(Category.Color),
                Icon = TrimOrNull(Category.Icon),
                SortOrder = Category.SortOrder,
                IsActive = Category.IsActive
            };

            _db.Categories.Add(category);
        }
        else
        {
            var existing = await _db.Categories
                .Where(c => c.Id == Category.Id && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                return NotFound();
            }

            existing.ParentCategoryId = Category.ParentCategoryId;
            existing.Name = Category.Name.Trim();
            existing.NameAr = Category.NameAr?.Trim() ?? string.Empty;
            existing.Description = TrimOrNull(Category.Description);
            existing.DescriptionAr = TrimOrNull(Category.DescriptionAr);
            existing.Color = TrimOrNull(Category.Color);
            existing.Icon = TrimOrNull(Category.Icon);
            existing.SortOrder = Category.SortOrder;
            existing.IsActive = Category.IsActive;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Category saved successfully.";
        return RedirectToPage("/Categories/Index");
    }

    private async Task LoadParentCategoriesAsync(Guid tenantId, Guid currentCategoryId)
    {
        ParentCategories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Id != currentCategoryId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new ParentCategoryOptionDto(c.Id, c.Name))
            .ToListAsync();
    }

    private async Task ValidateParentCategoryAsync(Guid tenantId)
    {
        if (!Category.ParentCategoryId.HasValue)
        {
            return;
        }

        var parentId = Category.ParentCategoryId.Value;
        var parentExists = await _db.Categories.AnyAsync(c => c.Id == parentId && c.TenantId == tenantId);
        if (!parentExists)
        {
            ModelState.AddModelError("Category.ParentCategoryId", "Selected parent category was not found.");
            return;
        }

        if (Category.Id == Guid.Empty)
        {
            return;
        }

        if (parentId == Category.Id)
        {
            ModelState.AddModelError("Category.ParentCategoryId", "A category cannot be its own parent.");
            return;
        }

        var currentParentId = parentId;
        while (currentParentId != null)
        {
            if (currentParentId == Category.Id)
            {
                ModelState.AddModelError("Category.ParentCategoryId", "A category cannot be assigned to one of its descendants.");
                return;
            }

            currentParentId = await _db.Categories
                .Where(c => c.Id == currentParentId.Value && c.TenantId == tenantId)
                .Select(c => c.ParentCategoryId)
                .FirstOrDefaultAsync();
        }
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class CategoryEditDto
{
    public Guid Id { get; set; }

    public Guid? ParentCategoryId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public string? DescriptionAr { get; set; }

    [StringLength(32)]
    public string? Color { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
