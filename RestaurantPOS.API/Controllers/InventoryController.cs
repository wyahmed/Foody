using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Shared.Models;

namespace RestaurantPOS.API.Controllers;

/// <summary>Inventory management API.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public InventoryController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get stock levels for the current branch.</summary>
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock([FromQuery] string? search, [FromQuery] Guid? warehouseId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var branchId = _currentUser.BranchId;
        var q = _db.StockItems.AsNoTracking().Where(s => s.Warehouse.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Product.Name.Contains(search));

        if (warehouseId.HasValue)
            q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(s => s.Product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                WarehouseName = s.Warehouse.Name,
                s.QuantityOnHand,
                s.MinStockLevel,
                s.ReorderLevel,
                IsLowStock = s.QuantityOnHand <= s.MinStockLevel
            })
            .ToListAsync();

        return Ok(PagedResult<object>.Create(items.Cast<object>().ToList().AsReadOnly(), total, page, pageSize));
    }

    /// <summary>Get low-stock items for the current branch.</summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var branchId = _currentUser.BranchId;
        var items = await _db.StockItems
            .AsNoTracking()
            .Where(s => s.Warehouse.BranchId == branchId && s.QuantityOnHand <= s.MinStockLevel)
            .Select(s => new
            {
                s.Id,
                ProductName = s.Product.Name,
                WarehouseName = s.Warehouse.Name,
                s.QuantityOnHand,
                s.MinStockLevel
            })
            .OrderBy(s => s.QuantityOnHand)
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Get stock movements for a product.</summary>
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] Guid? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var branchId = _currentUser.BranchId;
        var q = _db.StockMovements
            .AsNoTracking()
            .Where(m => m.Warehouse.BranchId == branchId);

        if (productId.HasValue)
            q = q.Where(m => m.ProductId == productId.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(m => m.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                ProductName = m.Product.Name,
                WarehouseName = m.Warehouse.Name,
                m.MovementType,
                m.Quantity,
                CostPerUnit = m.UnitCost,
                ReferenceNumber = m.Reference,
                m.Notes,
                m.CreatedDate
            })
            .ToListAsync();

        return Ok(PagedResult<object>.Create(items.Cast<object>().ToList().AsReadOnly(), total, page, pageSize));
    }

    /// <summary>Create a manual stock adjustment.</summary>
    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Manager,Inventory")]
    public async Task<IActionResult> Adjust([FromBody] StockAdjustmentRequest request)
    {
        var stockItem = await _db.StockItems
            .Where(s => s.ProductId == request.ProductId && s.WarehouseId == request.WarehouseId)
            .FirstOrDefaultAsync();

        if (stockItem == null) return NotFound("Stock item not found for this product/warehouse combination.");

        var before = stockItem.QuantityOnHand;
        stockItem.QuantityOnHand += request.Quantity;
        if (stockItem.QuantityOnHand < 0) stockItem.QuantityOnHand = 0;

        var movement = new RestaurantPOS.Domain.Entities.StockMovement
        {
            TenantId = _currentUser.TenantId ?? Guid.Empty,
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            MovementType = request.Quantity >= 0 ? RestaurantPOS.Domain.Enums.StockMovementType.Adjustment : RestaurantPOS.Domain.Enums.StockMovementType.Waste,
            Notes = request.Notes,
            BalanceBefore = before,
            BalanceAfter = stockItem.QuantityOnHand
        };

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(new { stockItem.QuantityOnHand, MovementId = movement.Id });
    }
}

public record StockAdjustmentRequest(Guid ProductId, Guid WarehouseId, decimal Quantity, string? Notes);
