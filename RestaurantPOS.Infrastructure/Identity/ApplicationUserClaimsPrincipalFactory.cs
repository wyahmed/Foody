using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>
/// Adds POS-specific user claims to cookie/JWT principals.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));
        identity.AddClaim(new Claim("BranchId", user.BranchId?.ToString() ?? string.Empty));
        identity.AddClaim(new Claim("FullName", user.FullName));
        return identity;
    }
}
