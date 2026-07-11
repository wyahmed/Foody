using Microsoft.AspNetCore.Http;
using RestaurantPOS.Domain.Interfaces;
using System.Security.Claims;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>
/// Retrieves the current user's identity context from the HTTP request.
/// Used throughout the application for auditing and tenant filtering.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null ? null : Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var id = User?.FindFirstValue("TenantId");
            return id is null ? null : Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public Guid? BranchId
    {
        get
        {
            var id = User?.FindFirstValue("BranchId");
            return id is null ? null : Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);
    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
    public bool HasPermission(string permission) => User?.HasClaim("Permission", permission) ?? false;
}
