using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestaurantPOS.Application.Interfaces;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Infrastructure.Identity;
using RestaurantPOS.Shared.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>
/// Concrete implementation of IIdentityService using ASP.NET Core Identity.
/// Handles login, password management, and JWT token generation.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public IdentityService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive || user.IsDeleted)
            return Result<AuthResult>.Failure("Invalid credentials.", "INVALID_CREDENTIALS");

        if (await _userManager.IsLockedOutAsync(user))
            return Result<AuthResult>.Failure("Account is locked. Try again later.", "ACCOUNT_LOCKED");

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<AuthResult>.Failure("Invalid credentials.", "INVALID_CREDENTIALS");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, refreshToken, expiresAt) = GenerateJwtToken(user, roles);

        return Result<AuthResult>.Success(new AuthResult(
            token, refreshToken, expiresAt,
            user.Id.ToString(), user.Email!,
            user.FullName,
            user.TenantId.ToString(),
            user.BranchId?.ToString(),
            roles,
            user.PreferredLanguage.ToString().ToLower()));
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Failure("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            BranchId = request.BranchId,
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FirstNameAr = request.FirstNameAr,
            LastNameAr = request.LastNameAr,
            EmployeeCode = request.EmployeeCode,
            IsActive = true,
            PreferredLanguage = Language.Arabic
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        return result.Succeeded
            ? Result<Guid>.Success(user.Id)
            : Result<Guid>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Failure("User not found.");
        var result = await _userManager.AddToRoleAsync(user, role);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.FullName, user.TenantId, user.BranchId, user.IsActive, roles);
    }

    private (string Token, string RefreshToken, DateTime ExpiresAt) GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "480");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("TenantId", user.TenantId.ToString()),
            new("BranchId", user.BranchId?.ToString() ?? ""),
            new("FullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), Convert.ToBase64String(Guid.NewGuid().ToByteArray()), expires);
    }
}
