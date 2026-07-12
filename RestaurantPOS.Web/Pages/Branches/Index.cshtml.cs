using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Branches;

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

    public List<BranchDto> Branches { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        Branches = await _db.Branches
            .AsNoTracking()
            .Where(b => b.ParentTenantId == tenantId)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                NameAr = b.NameAr,
                City = b.City,
                Address = b.Address,
                Phone = b.Phone,
                BranchType = b.BranchType.ToString(),
                IsActive = b.IsActive
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name, string? nameAr, string? city, string? address, string? phone, string? branchType)
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Branch name is required.";
            return RedirectToPage();
        }

        var type = Enum.TryParse<BranchType>(branchType, out var bt) ? bt : BranchType.Restaurant;

        var branch = new Branch
        {
            ParentTenantId = tenantId,
            Name = name.Trim(),
            NameAr = nameAr?.Trim() ?? name.Trim(),
            City = city?.Trim(),
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            BranchType = type,
            IsActive = true
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Branch added successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id, string name, string? nameAr, string? city, string? address, string? phone)
    {
        var tenantId = _currentUser.TenantId ?? Guid.Empty;

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.ParentTenantId == tenantId);

        if (branch is null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Branch name is required.";
            return RedirectToPage();
        }

        branch.Name = name.Trim();
        branch.NameAr = nameAr?.Trim() ?? branch.NameAr;
        branch.City = city?.Trim();
        branch.Address = address?.Trim();
        branch.Phone = phone?.Trim();

        await _db.SaveChangesAsync();

        TempData["Success"] = "Branch updated successfully.";
        return RedirectToPage();
    }
}

public class BranchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? City { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string BranchType { get; init; } = "";
    public bool IsActive { get; init; }
}
