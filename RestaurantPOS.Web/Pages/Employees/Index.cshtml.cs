using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Infrastructure.Identity;

namespace RestaurantPOS.Web.Pages.Employees;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ICurrentUserService currentUser)
    {
        _db = db;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public List<EmployeeDto> Employees { get; private set; } = new();
    public List<string> Roles { get; private set; } = new();

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? RoleFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
    public int TotalPages { get; private set; }

    private const int PageSize = 25;

    public async Task OnGetAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        // Load role names
        Roles = await _db.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Name!)
            .Where(n => n != null)
            .ToListAsync();

        // Load branches for name lookup
        var branches = await _db.Branches
            .AsNoTracking()
            .Where(b => b.ParentTenantId == tenantId)
            .Select(b => new { b.Id, b.Name })
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        // Query users
        var query = _userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(u =>
                u.FirstName.Contains(Search) ||
                (u.LastName != null && u.LastName.Contains(Search)) ||
                (u.Email != null && u.Email.Contains(Search)) ||
                (u.EmployeeCode != null && u.EmployeeCode.Contains(Search)));

        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));

        var users = await query
            .OrderBy(u => u.FirstName)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Get roles per user
        var result = new List<EmployeeDto>();
        foreach (var user in users)
        {
            var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            if (!string.IsNullOrEmpty(RoleFilter) && !userRoles.Contains(RoleFilter))
                continue;

            result.Add(new EmployeeDto
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email,
                EmployeeCode = user.EmployeeCode,
                BranchName = user.BranchId.HasValue && branches.TryGetValue(user.BranchId.Value, out var bn) ? bn : null,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                Roles = userRoles
            });
        }

        Employees = result;
    }
}

public class EmployeeDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = "";
    public string? Email { get; init; }
    public string? EmployeeCode { get; init; }
    public string? BranchName { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLoginDate { get; init; }
    public List<string> Roles { get; init; } = new();
}
