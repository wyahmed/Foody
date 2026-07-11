using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Sales invoice with ZATCA e-invoicing support.</summary>
public class Invoice : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Simplified;
    public string? InvoiceTypeCode { get; set; } = "388";
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public string? CustomerName { get; set; }
    public string? CustomerVat { get; set; }
    public string? CustomerCr { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? QrCode { get; set; }
    public string? XmlContent { get; set; }
    public string? XmlHash { get; set; }
    public string? DigitalSignature { get; set; }
    public ZatcaStatus ZatcaStatus { get; set; } = ZatcaStatus.Pending;
    public string? ZatcaWarnings { get; set; }
    public string? ZatcaErrors { get; set; }
    public string? ZatcaReportingRequestId { get; set; }
    public string? ZatcaClearanceRequestId { get; set; }
    public DateTime? ZatcaReportedAt { get; set; }
    public int ZatcaRetryCount { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? CreditNoteForInvoiceId { get; set; }
    public Guid? DebitNoteForInvoiceId { get; set; }

    public Order Order { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new HashSet<InvoiceItem>();
}

/// <summary>Invoice line item (snapshot at time of sale).</summary>
public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductNameAr { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Cash drawer session management.</summary>
public class Shift : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid CashierId { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public string ShiftNumber { get; set; } = string.Empty;
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CashDifference { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public int TotalOrders { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }

    public Branch Branch { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    public ICollection<CashDrawerTransaction> CashTransactions { get; set; } = new HashSet<CashDrawerTransaction>();
}

/// <summary>Individual cash drawer transaction (add/remove cash).</summary>
public class CashDrawerTransaction : BaseEntity
{
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public bool IsIn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public Shift Shift { get; set; } = null!;
}

/// <summary>Business expense record.</summary>
public class Expense : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid? ShiftId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Reference { get; set; }
    public string? ReceiptUrl { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public Shift? Shift { get; set; }
}

/// <summary>System-wide or branch-level settings key-value store.</summary>
public class Setting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public string? Group { get; set; }
    public string? DataType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}

/// <summary>Real-time notification record.</summary>
public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid BranchId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageAr { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public Branch Branch { get; set; } = null!;
}
