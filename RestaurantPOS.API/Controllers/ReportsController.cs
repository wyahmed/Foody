using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.API.Controllers;

/// <summary>Sales reports API.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager,Accountant,Auditor")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get sales summary for a date range.</summary>
    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var branchId = _currentUser.BranchId;
        var dateFrom = from?.Date ?? DateTime.Today;
        var dateTo = (to?.Date ?? DateTime.Today).AddDays(1);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId
                && o.Status == OrderStatus.Completed
                && o.CreatedDate >= dateFrom
                && o.CreatedDate < dateTo)
            .Select(o => new { o.TotalAmount, o.SubTotal, o.DiscountAmount, o.TaxAmount, o.OrderType })
            .ToListAsync();

        return Ok(new
        {
            DateFrom = dateFrom,
            DateTo = dateTo.AddDays(-1),
            TotalOrders = orders.Count,
            GrossSales = orders.Sum(o => o.SubTotal),
            TotalDiscount = orders.Sum(o => o.DiscountAmount),
            TotalVat = orders.Sum(o => o.TaxAmount),
            NetSales = orders.Sum(o => o.TotalAmount),
            AverageOrder = orders.Count > 0 ? orders.Sum(o => o.TotalAmount) / orders.Count : 0,
            ByType = orders.GroupBy(o => o.OrderType.ToString())
                .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                .OrderByDescending(x => x.Total)
        });
    }

    /// <summary>Get top selling products.</summary>
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 10)
    {
        var branchId = _currentUser.BranchId;
        var dateFrom = from?.Date ?? DateTime.Today.AddDays(-29);
        var dateTo = (to?.Date ?? DateTime.Today).AddDays(1);

        var products = await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.BranchId == branchId
                && i.Order.Status == OrderStatus.Completed
                && i.Order.CreatedDate >= dateFrom
                && i.Order.CreatedDate < dateTo)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>Get daily sales breakdown.</summary>
    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySales(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var branchId = _currentUser.BranchId;
        var dateFrom = from?.Date ?? DateTime.Today.AddDays(-29);
        var dateTo = (to?.Date ?? DateTime.Today).AddDays(1);

        var dailySales = await _db.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId
                && o.Status == OrderStatus.Completed
                && o.CreatedDate >= dateFrom
                && o.CreatedDate < dateTo)
            .GroupBy(o => o.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                OrderCount = g.Count(),
                GrossSales = g.Sum(o => o.SubTotal),
                Discount = g.Sum(o => o.DiscountAmount),
                Vat = g.Sum(o => o.TaxAmount),
                NetSales = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(dailySales);
    }
}
