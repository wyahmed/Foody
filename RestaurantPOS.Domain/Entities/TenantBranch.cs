using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Represents a tenant (business/company) in the system.</summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? VatNumber { get; set; }
    public string? CommercialRegistration { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "SA";
    public string? Currency { get; set; } = "SAR";
    public string? DefaultLanguage { get; set; } = "ar";
    public bool IsActive { get; set; } = true;
    public string? ZatcaEnvironment { get; set; } = "Sandbox";
    public string? ZatcaCsid { get; set; }
    public string? ZatcaPrivateKey { get; set; }
    public string? ZatcaPublicKey { get; set; }
    public string? ZatcaOrganizationalUnitName { get; set; }
    public DateTime? ZatcaLastCertificateDate { get; set; }

    public ICollection<Branch> Branches { get; set; } = new HashSet<Branch>();
}

/// <summary>Represents a branch of the restaurant/grocery.</summary>
public class Branch : BaseEntity
{
    public Guid ParentTenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public BranchType BranchType { get; set; } = BranchType.Restaurant;
    public bool IsActive { get; set; } = true;
    public bool HasKitchenDisplay { get; set; }
    public bool HasCustomerDisplay { get; set; }
    public decimal? DefaultTaxRate { get; set; }
    public decimal? ServiceChargeRate { get; set; }
    public string? TimeZone { get; set; } = "Arab Standard Time";
    public string? Currency { get; set; } = "SAR";

    public Tenant Tenant { get; set; } = null!;
    public ICollection<DiningTable> Tables { get; set; } = new HashSet<DiningTable>();
    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    public ICollection<Shift> Shifts { get; set; } = new HashSet<Shift>();
    public ICollection<Warehouse> Warehouses { get; set; } = new HashSet<Warehouse>();
    public ICollection<BranchProduct> BranchProducts { get; set; } = new HashSet<BranchProduct>();
}
