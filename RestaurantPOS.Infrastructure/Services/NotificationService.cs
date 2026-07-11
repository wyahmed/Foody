using Microsoft.AspNetCore.SignalR;
using RestaurantPOS.Domain.Interfaces;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>
/// Real-time notification service using ASP.NET Core SignalR.
/// Broadcasts events to connected clients by user or branch group.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<PosHub> _hubContext;

    public NotificationService(IHubContext<PosHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken = default)
        => _hubContext.Clients.User(userId).SendAsync(eventName, payload, cancellationToken);

    public Task SendToBranchAsync(string branchId, string eventName, object payload, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"branch-{branchId}").SendAsync(eventName, payload, cancellationToken);

    public Task SendToAllAsync(string eventName, object payload, CancellationToken cancellationToken = default)
        => _hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken);
}

/// <summary>
/// SignalR hub for real-time POS events (new orders, kitchen updates, low stock alerts).
/// Clients join branch-specific groups for targeted delivery.
/// </summary>
public class PosHub : Hub
{
    public async Task JoinBranch(string branchId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");

    public async Task LeaveBranch(string branchId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch-{branchId}");

    public async Task JoinKitchen(string branchId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"kitchen-{branchId}");

    public async Task LeaveKitchen(string branchId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kitchen-{branchId}");
}
