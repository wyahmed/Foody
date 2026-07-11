using Microsoft.AspNetCore.Identity;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>Extends ASP.NET Identity user with POS-specific fields.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FirstNameAr { get; set; }
    public string? LastNameAr { get; set; }
    public string? EmployeeCode { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? NationalId { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public Language PreferredLanguage { get; set; } = Language.Arabic;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
    public string? LastLoginIp { get; set; }
    public string? LastLoginDevice { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
    public ICollection<LoginHistory> LoginHistories { get; set; } = new HashSet<LoginHistory>();
}

/// <summary>Extends ASP.NET Identity role with tenant and permission support.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
}

/// <summary>Maps roles to granular permissions.</summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public bool IsGranted { get; set; } = true;

    public ApplicationRole Role { get; set; } = null!;
}

/// <summary>Audit log entry for every significant action.</summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public ApplicationUser? User { get; set; }
}

/// <summary>Tracks login history per user.</summary>
public class LoginHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public DateTime LoginDate { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
