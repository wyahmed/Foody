using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Web.Extensions;

namespace RestaurantPOS.Web.Pages.Products;

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

    [BindProperty] public ProductEditDto Product { get; set; } = new();

    public List<CategoryOptionDto> Categories { get; set; } = new();
    public List<BrandOptionDto> Brands { get; set; } = new();
    public List<UnitOptionDto> Units { get; set; } = new();
    public List<TaxRateOptionDto> TaxRates { get; set; } = new();
    public bool IsNew => Product.Id == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        await LoadLookupsAsync();

        if (id.HasValue && id.Value != Guid.Empty)
        {
            var product = await _db.Products.AsNoTracking()
                .Where(p => p.Id == id.Value && p.TenantId == (_currentUser.TenantId ?? Guid.Empty))
                .FirstOrDefaultAsync();

            if (product == null) return NotFound();

            Product = new ProductEditDto
            {
                Id = product.Id,
                Name = product.Name,
                NameAr = product.NameAr,
                Description = product.Description,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                UnitId = product.UnitId,
                TaxRateId = product.TaxRateId,
                SKU = product.SKU,
                Barcode = product.Barcode,
                SellingPrice = product.SellingPrice,
                CostPrice = product.CostPrice,
                IsActive = product.IsActive,
                TrackInventory = product.TrackInventory,
                ImageUrl = product.ImageUrl,
                MinStockLevel = product.MinStockLevel,
                MaxStockLevel = product.MaxStockLevel
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? imageFile)
    {
        await LoadLookupsAsync();

        if (!ModelState.IsValid) return Page();

        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        string? imageUrl = Product.ImageUrl;
        if (imageFile?.Length > 0)
        {
            // TODO: Integrate cloud storage (Azure Blob, S3, or local). Store URL.
            // For now, save to wwwroot/uploads
            var uploadsDir = Path.Combine("wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await imageFile.CopyToAsync(stream);
            imageUrl = $"/uploads/products/{fileName}";
        }

        if (Product.Id == Guid.Empty)
        {
            // Create new product
            var product = new Product
            {
                TenantId = tenantId,
                Name = Product.Name.Trim(),
                NameAr = Product.NameAr?.Trim() ?? string.Empty,
                Description = Product.Description?.Trim(),
                CategoryId = Product.CategoryId,
                BrandId = Product.BrandId,
                UnitId = Product.UnitId,
                TaxRateId = Product.TaxRateId,
                SKU = Product.SKU?.Trim(),
                Barcode = Product.Barcode?.Trim(),
                SellingPrice = Product.SellingPrice,
                CostPrice = Product.CostPrice,
                IsActive = Product.IsActive,
                TrackInventory = Product.TrackInventory,
                ImageUrl = imageUrl,
                MinStockLevel = Product.MinStockLevel,
                MaxStockLevel = Product.MaxStockLevel
            };

            _db.Products.Add(product);
        }
        else
        {
            // Update existing product
            var existing = await _db.Products
                .Where(p => p.Id == Product.Id && p.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (existing == null) return NotFound();

            existing.Name = Product.Name.Trim();
            existing.NameAr = Product.NameAr?.Trim() ?? string.Empty;
            existing.Description = Product.Description?.Trim();
            existing.CategoryId = Product.CategoryId;
            existing.BrandId = Product.BrandId;
            existing.UnitId = Product.UnitId;
            existing.TaxRateId = Product.TaxRateId;
            existing.SKU = Product.SKU?.Trim();
            existing.Barcode = Product.Barcode?.Trim();
            existing.SellingPrice = Product.SellingPrice;
            existing.CostPrice = Product.CostPrice;
            existing.IsActive = Product.IsActive;
            existing.TrackInventory = Product.TrackInventory;
            existing.MinStockLevel = Product.MinStockLevel;
            existing.MaxStockLevel = Product.MaxStockLevel;
            if (imageUrl != null) existing.ImageUrl = imageUrl;
        }

        await _db.SaveChangesAsync();
        this.SetSuccessMessage("Product saved successfully.");
        return RedirectToPage("/Products/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        var product = await _db.Products
            .Where(p => p.Id == Product.Id && p.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (product != null) { product.IsDeleted = true; await _db.SaveChangesAsync(); }
        this.SetSuccessMessage("Product deleted.");
        return RedirectToPage("/Products/Index");
    }

    private async Task LoadLookupsAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        Categories = await _db.Categories.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Select(c => new CategoryOptionDto { Id = c.Id, Name = c.Name })
            .OrderBy(c => c.Name).ToListAsync();

        Brands = await _db.Brands.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .Select(b => new BrandOptionDto { Id = b.Id, Name = b.Name })
            .OrderBy(b => b.Name).ToListAsync();

        Units = await _db.Units.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => new UnitOptionDto { Id = u.Id, Name = u.Name })
            .OrderBy(u => u.Name).ToListAsync();

        TaxRates = await _db.TaxRates.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Select(t => new TaxRateOptionDto { Id = t.Id, Name = t.Name, Rate = t.Rate })
            .OrderBy(t => t.Name).ToListAsync();
    }
}

public class ProductEditDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
    public string? ImageUrl { get; set; }
    public decimal? MinStockLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
}

public record BrandOptionDto { public Guid Id { get; init; } public string Name { get; init; } = ""; }
public record UnitOptionDto { public Guid Id { get; init; } public string Name { get; init; } = ""; }
public record TaxRateOptionDto { public Guid Id { get; init; } public string Name { get; init; } = ""; public decimal Rate { get; init; } }
