using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Infrastructure.Identity;

namespace RestaurantPOS.Web.Pages.Dashboard;

/// <summary>Dashboard page model – aggregates key metrics for the current branch.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    // --- Stats ---
    public decimal TodaySales { get; private set; }
    public int TodayOrders { get; private set; }
    public int PendingOrders { get; private set; }
    public int TodayCustomers { get; private set; }
    public int LowStockCount { get; private set; }
    public double SalesGrowth { get; private set; }

    // --- Table stats ---
    public int AvailableTables { get; private set; }
    public int OccupiedTables { get; private set; }
    public int ReservedTables { get; private set; }

    // --- Lists ---
    public List<RecentOrderDto> RecentOrders { get; private set; } = new();
    public List<TopProductDto> TopProducts { get; private set; } = new();
    public List<KitchenQueueDto> KitchenOrders { get; private set; } = new();
    public string? BranchId { get; private set; }

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;
        BranchId = branchId?.ToString();
        if (branchId is null) return;

        var today = DateTime.UtcNow.Date;
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        // Today's sales summary
        var todayOrders = await _context.Orders
            .Where(o => o.BranchId == branchId && o.CreatedDate >= today
                        && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        TodaySales = todayOrders.Sum(o => o.TotalAmount);
        TodayOrders = todayOrders.Count;
        PendingOrders = todayOrders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing);
        TodayCustomers = todayOrders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId).Distinct().Count();

        // Previous day comparison
        var yesterday = today.AddDays(-1);
        var yesterdaySales = await _context.Orders
            .Where(o => o.BranchId == branchId && o.CreatedDate >= yesterday && o.CreatedDate < today
                        && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount);

        SalesGrowth = yesterdaySales > 0
            ? Math.Round((double)((TodaySales - yesterdaySales) / yesterdaySales * 100), 1)
            : 0;

        // Recent orders
        RecentOrders = await _context.Orders
            .Include(o => o.Table)
            .Where(o => o.BranchId == branchId)
            .OrderByDescending(o => o.CreatedDate)
            .Take(10)
            .Select(o => new RecentOrderDto(
                o.Id, o.OrderNumber, o.CustomerName, o.OrderType.ToString(),
                o.TotalAmount, o.Status, o.CreatedDate))
            .ToListAsync();

        // Top products today
        var topProductsRaw = await _context.OrderItems
            .Include(i => i.Order)
            .Include(i => i.Product)
            .Where(i => i.Order.BranchId == branchId && i.Order.CreatedDate >= today && !i.IsVoid)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductName,
                Quantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        var maxRevenue = topProductsRaw.Any() ? topProductsRaw.Max(x => x.Revenue) : 1;
        TopProducts = topProductsRaw
            .Select(x => new TopProductDto(
                x.ProductName,
                (int)x.Quantity,
                x.Revenue,
                maxRevenue > 0 ? (int)Math.Round(x.Revenue / maxRevenue * 100) : 0))
            .ToList();

        // Table status
        var tables = await _context.DiningTables
            .Where(t => t.BranchId == branchId)
            .ToListAsync();
        AvailableTables = tables.Count(t => t.Status == TableStatus.Available);
        OccupiedTables = tables.Count(t => t.Status == TableStatus.Occupied);
        ReservedTables = tables.Count(t => t.Status == TableStatus.Reserved);

        // Kitchen queue
        KitchenOrders = await _context.KitchenOrders
            .Where(k => k.BranchId == branchId &&
                        (k.Status == KitchenOrderStatus.New || k.Status == KitchenOrderStatus.Preparing))
            .OrderBy(k => k.CreatedDate)
            .Take(6)
            .Select(k => new KitchenQueueDto(
                k.OrderNumber,
                k.Status.ToString(),
                k.CreatedDate))
            .ToListAsync();

        // Low stock
        LowStockCount = await _context.StockItems
            .Include(s => s.Product)
            .CountAsync(s => s.Warehouse.BranchId == branchId
                             && s.Product.TrackInventory
                             && s.Product.MinStockLevel.HasValue
                             && s.QuantityOnHand <= s.Product.MinStockLevel);
    }

    public record RecentOrderDto(Guid Id, string OrderNumber, string? CustomerName, string OrderType,
        decimal TotalAmount, OrderStatus Status, DateTime CreatedDate);
    public record TopProductDto(string Name, int Quantity, decimal Revenue, int Percentage);
    public record KitchenQueueDto(string OrderNumber, string Status, DateTime CreatedAt)
    {
        public string CreatedAgo =>
            (DateTime.UtcNow - CreatedAt).TotalMinutes < 60
                ? $"{(int)(DateTime.UtcNow - CreatedAt).TotalMinutes} min ago"
                : $"{(int)(DateTime.UtcNow - CreatedAt).TotalHours}h ago";
    }
}
