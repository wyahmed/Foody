using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;

namespace RestaurantPOS.Web.Pages.Settings;

[Authorize(Roles = "SuperAdmin,Admin,Owner")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    private Dictionary<string, string> _settings = new();

    public IndexModel(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string GetSetting(string key, string defaultValue = "") =>
        _settings.TryGetValue(key, out var val) ? val : defaultValue;

    public async Task OnGetAsync()
    {
        var branchId = _currentUser.BranchId ?? Guid.Empty;
        _settings = await _db.Settings
            .AsNoTracking()
            .Where(s => s.BranchId == branchId)
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);
    }

    public async Task<IActionResult> OnPostSaveBusinessAsync(
        string? businessName, string? businessNameAr, string? vatNumber, string? crNumber, string? phone, string? address)
    {
        await SaveSettingsAsync(new Dictionary<string, string?>
        {
            ["BusinessName"] = businessName,
            ["BusinessNameAr"] = businessNameAr,
            ["VatNumber"] = vatNumber,
            ["CRNumber"] = crNumber,
            ["Phone"] = phone,
            ["Address"] = address
        });
        TempData["Success"] = "Business settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveTaxAsync(
        string? defaultVatRate, string? serviceCharge, bool pricesIncludeVat, string? currency)
    {
        await SaveSettingsAsync(new Dictionary<string, string?>
        {
            ["DefaultVatRate"] = defaultVatRate,
            ["ServiceCharge"] = serviceCharge,
            ["PricesIncludeVat"] = pricesIncludeVat.ToString().ToLower(),
            ["Currency"] = currency
        });
        TempData["Success"] = "Tax settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveReceiptAsync(string? receiptFooter, bool printQrCode, bool autoPrint)
    {
        await SaveSettingsAsync(new Dictionary<string, string?>
        {
            ["ReceiptFooter"] = receiptFooter,
            ["PrintQrCode"] = printQrCode.ToString().ToLower(),
            ["AutoPrint"] = autoPrint.ToString().ToLower()
        });
        TempData["Success"] = "Receipt settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveZatcaAsync(
        string? zatcaSellerName, string? zatcaCertificate, string? zatcaPrivateKey, bool zatcaSimulation)
    {
        await SaveSettingsAsync(new Dictionary<string, string?>
        {
            ["ZatcaSellerName"] = zatcaSellerName,
            ["ZatcaCertificate"] = zatcaCertificate,
            ["ZatcaPrivateKey"] = zatcaPrivateKey,
            ["ZatcaSimulation"] = zatcaSimulation.ToString().ToLower()
        });
        TempData["Success"] = "ZATCA settings saved.";
        return RedirectToPage();
    }

    private async Task SaveSettingsAsync(Dictionary<string, string?> pairs)
    {
        var branchId = _currentUser.BranchId ?? Guid.Empty;
        var tenantId = _currentUser.TenantId ?? Guid.Empty;
        var existingSettings = await _db.Settings
            .Where(s => s.BranchId == branchId && pairs.Keys.Contains(s.Key))
            .ToListAsync();

        foreach (var (key, value) in pairs)
        {
            if (value == null) continue;
            var existing = existingSettings.FirstOrDefault(s => s.Key == key);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                _db.Settings.Add(new Setting
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    Key = key,
                    Value = value
                });
            }
        }
        await _db.SaveChangesAsync();
    }
}
