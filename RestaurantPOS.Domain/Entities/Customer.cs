using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Customer record with CRM data.</summary>
public class Customer : BaseEntity
{
    public string? Code { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal StoreCredit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastOrderDate { get; set; }
    public string? VatNumber { get; set; }
    public string? CommercialRegistration { get; set; }

    public CustomerGroup? CustomerGroup { get; set; }
    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new HashSet<LoyaltyTransaction>();
    public ICollection<GiftCard> GiftCards { get; set; } = new HashSet<GiftCard>();
    public ICollection<TableReservation> Reservations { get; set; } = new HashSet<TableReservation>();
}

/// <summary>Customer segment/group for targeted promotions.</summary>
public class CustomerGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
}

/// <summary>Loyalty program configuration.</summary>
public class LoyaltyProgram : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal PointsPerSar { get; set; } = 1;
    public decimal SarPerPoint { get; set; } = 0.1m;
    public int? MinPointsToRedeem { get; set; }
    public int? MaxPointsPerOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Loyalty points transaction per customer.</summary>
public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public LoyaltyTransactionType TransactionType { get; set; }
    public int Points { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
}

/// <summary>Gift card.</summary>
public class GiftCard : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Customer? Customer { get; set; }
}

/// <summary>Supplier for purchasing inventory.</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? VatNumber { get; set; }
    public string? CommercialRegistration { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermDays { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal OutstandingBalance { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new HashSet<PurchaseOrder>();
    public ICollection<SupplierInvoice> Invoices { get; set; } = new HashSet<SupplierInvoice>();
}
