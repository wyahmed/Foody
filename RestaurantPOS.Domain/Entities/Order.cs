using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Sales order (dine-in, take-away, delivery, etc.).</summary>
public class Order : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid? CashierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? TableId { get; set; }
    public Guid? ShiftId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? Notes { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal DeliveryChargeAmount { get; set; }
    public decimal TipsAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public Guid? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public Guid? DiscountId { get; set; }
    public DateTime? EstimatedReadyTime { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int NumberOfGuests { get; set; } = 1;
    public int? OriginalOrderId { get; set; }
    public bool IsSplit { get; set; }
    public bool IsMerged { get; set; }

    public Branch Branch { get; set; } = null!;
    public Customer? Customer { get; set; }
    public DiningTable? Table { get; set; }
    public Shift? Shift { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    public Invoice? Invoice { get; set; }
    public KitchenOrder? KitchenOrder { get; set; }
}

/// <summary>Individual line item on an order.</summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductNameAr { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public bool IsVoid { get; set; }
    public string? VoidReason { get; set; }
    public KitchenOrderStatus KitchenStatus { get; set; } = KitchenOrderStatus.New;

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
    public ICollection<OrderItemModifier> Modifiers { get; set; } = new HashSet<OrderItemModifier>();
}

/// <summary>Modifier applied to an order item.</summary>
public class OrderItemModifier : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public Guid ModifierId { get; set; }
    public string ModifierName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; } = 1;

    public OrderItem OrderItem { get; set; } = null!;
    public Modifier Modifier { get; set; } = null!;
}

/// <summary>Payment applied to an order (supports split payments).</summary>
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Reference { get; set; }
    public string? CardLast4 { get; set; }
    public string? Notes { get; set; }
    public bool IsVoid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}

/// <summary>Discount definition (percentage or fixed amount).</summary>
public class Discount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ApplyToAll { get; set; } = true;
    public bool RequiresManagerApproval { get; set; }
}

/// <summary>Coupon code for order discounts.</summary>
public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOneTimeUse { get; set; }
    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }
}

/// <summary>Happy hour pricing configuration.</summary>
public class HappyHour : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<HappyHourProduct> Products { get; set; } = new HashSet<HappyHourProduct>();
}

/// <summary>Links happy hour to specific products.</summary>
public class HappyHourProduct : BaseEntity
{
    public Guid HappyHourId { get; set; }
    public Guid ProductId { get; set; }

    public HappyHour HappyHour { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
