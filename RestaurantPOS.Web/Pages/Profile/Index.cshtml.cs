using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Infrastructure.Identity;

namespace RestaurantPOS.Web.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _db = db;
        _currentUser = currentUser;
    }

    public string FullName { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string? LastName { get; private set; }
    public string? FirstNameAr { get; private set; }
    public string? LastNameAr { get; private set; }
    public string? Email { get; private set; }
    public string? EmployeeCode { get; private set; }
    public string? BranchName { get; private set; }
    public List<string> Roles { get; private set; } = new();
    public DateTime? LastLogin { get; private set; }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = _currentUser.UserId;
        if (userId is null) return null;
        return await _userManager.FindByIdAsync(userId.ToString()!);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return RedirectToPage("/Account/Login");

        await LoadProfileAsync(user);
        return Page();
    }

    private async Task LoadProfileAsync(ApplicationUser user)
    {
        FirstName = user.FirstName;
        LastName = user.LastName;
        FirstNameAr = user.FirstNameAr;
        LastNameAr = user.LastNameAr;
        FullName = user.FullName.Length > 0 ? user.FullName : user.UserName ?? "User";
        Email = user.Email;
        EmployeeCode = user.EmployeeCode;
        LastLogin = user.LastLoginDate;
        Roles = (await _userManager.GetRolesAsync(user)).ToList();

        if (user.BranchId.HasValue)
        {
            var branch = await _db.Branches
                .AsNoTracking()
                .Where(b => b.Id == user.BranchId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync();
            BranchName = branch;
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync(
        string firstName, string? lastName, string? firstNameAr, string? lastNameAr)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(firstName))
        {
            TempData["Error"] = "First name is required.";
            return RedirectToPage();
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName?.Trim() ?? "";
        user.FirstNameAr = firstNameAr?.Trim();
        user.LastNameAr = lastNameAr?.Trim();

        var result = await _userManager.UpdateAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Profile updated successfully." : string.Join("; ", result.Errors.Select(e => e.Description));

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(
        string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return RedirectToPage("/Account/Login");

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "New password and confirmation do not match.";
            return RedirectToPage();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Password changed successfully." : string.Join("; ", result.Errors.Select(e => e.Description));

        return RedirectToPage();
    }
}
