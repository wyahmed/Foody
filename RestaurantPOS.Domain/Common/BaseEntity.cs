using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Domain.Common;

/// <summary>
/// Base entity with audit fields, soft delete, multi-tenant, and concurrency token support.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the tenant identifier for multi-tenancy.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the date/time when this record was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the user who last updated this record.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets the date/time when this record was last updated.</summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>Gets or sets a value indicating whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the date/time when this record was deleted.</summary>
    public DateTime? DeletedDate { get; set; }

    /// <summary>Gets or sets the user who deleted this record.</summary>
    public string? DeletedBy { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
