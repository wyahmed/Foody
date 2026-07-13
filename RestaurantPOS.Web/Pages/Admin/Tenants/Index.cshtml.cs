using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Shared.Constants;

namespace RestaurantPOS.Web.Pages.Admin.Tenants;

[Authorize(Roles = Roles.SuperAdmin)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public List<TenantDto> Tenants { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Tenants = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                NameAr = t.NameAr,
                VatNumber = t.VatNumber,
                CommercialRegistration = t.CommercialRegistration,
                Phone = t.Phone,
                Email = t.Email,
                Address = t.Address,
                City = t.City,
                Country = t.Country,
                Currency = t.Currency,
                DefaultLanguage = t.DefaultLanguage,
                IsActive = t.IsActive,
                ZatcaEnvironment = t.ZatcaEnvironment,
                CreatedDate = t.CreatedDate
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name, string? nameAr, string? vatNumber, string? commercialRegistration,
        string? phone, string? email, string? address, string? city,
        string? country, string? currency, string? defaultLanguage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tenant name is required.";
            return RedirectToPage();
        }

        var tenant = new Tenant
        {
            Name = name.Trim(),
            NameAr = nameAr?.Trim() ?? name.Trim(),
            VatNumber = vatNumber?.Trim(),
            CommercialRegistration = commercialRegistration?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            City = city?.Trim(),
            Country = country?.Trim() ?? "SA",
            Currency = currency?.Trim() ?? "SAR",
            DefaultLanguage = defaultLanguage?.Trim() ?? "ar",
            IsActive = true,
            ZatcaEnvironment = "Sandbox"
        };
        // Tenant owns itself
        tenant.TenantId = tenant.Id;

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Tenant '{tenant.Name}' created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id, string name, string? nameAr, string? vatNumber, string? commercialRegistration,
        string? phone, string? email, string? address, string? city,
        string? country, string? currency, string? defaultLanguage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tenant name is required.";
            return RedirectToPage();
        }

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null) return NotFound();

        tenant.Name = name.Trim();
        tenant.NameAr = nameAr?.Trim() ?? tenant.NameAr;
        tenant.VatNumber = vatNumber?.Trim();
        tenant.CommercialRegistration = commercialRegistration?.Trim();
        tenant.Phone = phone?.Trim();
        tenant.Email = email?.Trim();
        tenant.Address = address?.Trim();
        tenant.City = city?.Trim();
        tenant.Country = country?.Trim() ?? tenant.Country;
        tenant.Currency = currency?.Trim() ?? tenant.Currency;
        tenant.DefaultLanguage = defaultLanguage?.Trim() ?? tenant.DefaultLanguage;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Tenant '{tenant.Name}' updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null) return NotFound();

        tenant.IsActive = !tenant.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Tenant '{tenant.Name}' is now {(tenant.IsActive ? "active" : "inactive")}.";
        return RedirectToPage();
    }
}

public class TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? VatNumber { get; init; }
    public string? CommercialRegistration { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Currency { get; init; }
    public string? DefaultLanguage { get; init; }
    public bool IsActive { get; init; }
    public string? ZatcaEnvironment { get; init; }
    public DateTime CreatedDate { get; init; }
}
