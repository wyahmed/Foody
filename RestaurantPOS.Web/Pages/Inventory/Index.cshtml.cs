using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Inventory;

[Authorize(Roles = "Admin,Manager,Inventory")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? Filter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public List<StockItemDto> Items { get; set; } = new();
    public List<WarehouseOptionDto> Warehouses { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int LowStockCount { get; set; }
    private const int PageSize = 30;

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;

        Warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => w.BranchId == branchId)
            .Select(w => new WarehouseOptionDto { Id = w.Id, Name = w.Name })
            .ToListAsync();

        var q = _db.StockItems
            .AsNoTracking()
            .Where(s => s.Warehouse.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(s => s.Product.Name.Contains(Search));

        if (WarehouseId.HasValue)
            q = q.Where(s => s.WarehouseId == WarehouseId.Value);

        if (Filter == "low")
            q = q.Where(s => s.QuantityOnHand > 0 && s.QuantityOnHand <= s.MinStockLevel);
        else if (Filter == "out")
            q = q.Where(s => s.QuantityOnHand <= 0);

        LowStockCount = await _db.StockItems
            .AsNoTracking()
            .CountAsync(s => s.Warehouse.BranchId == branchId && s.QuantityOnHand <= s.MinStockLevel);

        TotalCount = await q.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));

        Items = await q
            .OrderBy(s => s.Product.Name)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(s => new StockItemDto
            {
                ProductName = s.Product.Name,
                WarehouseName = s.Warehouse.Name,
                UnitName = s.Product.Unit != null ? s.Product.Unit.Name : "Unit",
                QuantityOnHand = s.QuantityOnHand,
                MinStockLevel = s.MinStockLevel,
                ReorderLevel = s.ReorderLevel,
                UpdatedDate = s.UpdatedDate ?? DateTime.UtcNow
            })
            .ToListAsync();
    }
}

public record StockItemDto
{
    public string ProductName { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public string UnitName { get; init; } = "";
    public decimal QuantityOnHand { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal ReorderLevel { get; init; }
    public DateTime UpdatedDate { get; init; }
}

public record WarehouseOptionDto { public Guid Id { get; init; } public string Name { get; init; } = ""; }
