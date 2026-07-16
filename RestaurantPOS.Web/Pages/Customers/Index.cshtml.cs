using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Web.Extensions;

namespace RestaurantPOS.Web.Pages.Customers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public List<CustomerRowDto> Customers { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    private const int PageSize = 25;

    public async Task OnGetAsync()
    {
        var tenantId = _currentUser.TenantId;

        var q = _db.Customers.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(c => c.FirstName.Contains(Search) ||
                (c.LastName != null && c.LastName.Contains(Search)) ||
                (c.Phone != null && c.Phone.Contains(Search)) ||
                (c.Email != null && c.Email.Contains(Search)));

        TotalCount = await q.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        Page = Math.Max(1, Math.Min(Page, Math.Max(1, TotalPages)));

        Customers = await q
            .OrderBy(c => c.FirstName)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CustomerRowDto
            {
                Id = c.Id,
                Name = c.FirstName + (c.LastName != null ? " " + c.LastName : ""),
                Phone = c.Phone,
                Email = c.Email,
                LoyaltyPoints = c.LoyaltyPoints,
                TotalSpent = c.TotalSpent,
                TotalOrders = c.TotalOrders,
                GroupName = c.CustomerGroup != null ? c.CustomerGroup.Name : null
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string firstName, string? lastName, string? phone, string? email)
    {
        var tenantId = _currentUser.TenantId;
        var branchId = _currentUser.BranchId;

        if (string.IsNullOrWhiteSpace(firstName))
            return BadRequest(this.T("First name is required."));

        var customer = new Customer
        {
            TenantId = tenantId ?? Guid.Empty,
            FirstName = firstName.Trim(),
            LastName = lastName?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim()
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        this.SetSuccessMessage("Customer added successfully.");
        return RedirectToPage();
    }
}

public record CustomerRowDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public int LoyaltyPoints { get; init; }
    public decimal TotalSpent { get; init; }
    public int TotalOrders { get; init; }
    public string? GroupName { get; init; }
}
