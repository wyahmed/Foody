using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Infrastructure.Services;

namespace RestaurantPOS.Web.Pages.Kitchen;

[Authorize(Roles = "Admin,Manager,Kitchen,Cashier")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<PosHub> _hub;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser, IHubContext<PosHub> hub)
    {
        _db = db;
        _currentUser = currentUser;
        _hub = hub;
    }

    public List<KitchenOrderDto> KitchenOrders { get; set; } = new();
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId;

        KitchenOrders = await _db.KitchenOrders
            .AsNoTracking()
            .Where(ko => ko.BranchId == branchId && ko.Status != KitchenOrderStatus.Served)
            .OrderBy(ko => ko.CreatedDate)
            .Select(ko => new KitchenOrderDto
            {
                Id = ko.Id,
                OrderNumber = ko.Order.OrderNumber,
                OrderType = ko.Order.OrderType.ToString(),
                Status = ko.Status.ToString(),
                TableName = ko.Order.Table != null ? ko.Order.Table.Name : null,
                CreatedDate = ko.CreatedDate,
                Items = ko.Order.Items.Select(i => new KitchenItemDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Notes = i.Notes,
                    IsCompleted = false
                }).ToList()
            })
            .ToListAsync();

        PendingCount = KitchenOrders.Count(x => x.Status == "New");
        PreparingCount = KitchenOrders.Count(x => x.Status == "Preparing");
    }

    public async Task<IActionResult> OnPostStartPreparingAsync(Guid id)
    {
        var ko = await _db.KitchenOrders.FindAsync(id);
        if (ko == null) return NotFound();

        ko.Status = KitchenOrderStatus.Preparing;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(_currentUser.BranchId.ToString())
            .SendAsync("KitchenOrderUpdated", id);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkReadyAsync(Guid id)
    {
        var ko = await _db.KitchenOrders.FindAsync(id);
        if (ko == null) return NotFound();

        ko.Status = KitchenOrderStatus.Ready;
        ko.CompletedAt = DateTime.UtcNow;

        // Update linked order status
        var order = await _db.Orders.FindAsync(ko.OrderId);
        if (order != null) order.Status = OrderStatus.Ready;

        await _db.SaveChangesAsync();

        await _hub.Clients.Group(_currentUser.BranchId.ToString())
            .SendAsync("KitchenReady", new { ko.OrderId, KitchenOrderId = id });

        return RedirectToPage();
    }
}

public record KitchenOrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string OrderType { get; init; } = "";
    public string Status { get; init; } = "";
    public string? TableName { get; init; }
    public DateTime CreatedDate { get; init; }
    public List<KitchenItemDto> Items { get; init; } = new();
}

public record KitchenItemDto
{
    public string ProductName { get; init; } = "";
    public decimal Quantity { get; init; }
    public string? Notes { get; init; }
    public bool IsCompleted { get; init; }
}
