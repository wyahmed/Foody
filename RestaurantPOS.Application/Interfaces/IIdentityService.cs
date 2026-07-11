using RestaurantPOS.Shared.Common;

namespace RestaurantPOS.Application.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Identity for use in Application layer commands/queries.
/// Implemented in Infrastructure to avoid direct dependency on Identity packages.
/// </summary>
public interface IIdentityService
{
    Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string FullName,
    string? TenantId,
    string? BranchId,
    IList<string> Roles,
    string PreferredLanguage);

public record CreateUserRequest(
    Guid TenantId,
    Guid? BranchId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? FirstNameAr,
    string? LastNameAr,
    string? EmployeeCode);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    Guid TenantId,
    Guid? BranchId,
    bool IsActive,
    IList<string> Roles);
