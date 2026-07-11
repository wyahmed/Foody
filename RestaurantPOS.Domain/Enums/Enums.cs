namespace RestaurantPOS.Domain.Enums;

public enum OrderType
{
    DineIn = 1,
    TakeAway = 2,
    Delivery = 3,
    DriveThrough = 4
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Preparing = 3,
    Ready = 4,
    Served = 5,
    Completed = 6,
    Cancelled = 7,
    Refunded = 8
}

public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4,
    Voided = 5
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    GiftCard = 4,
    StoreCredit = 5,
    LoyaltyPoints = 6,
    SplitPayment = 7
}

public enum TableStatus
{
    Available = 1,
    Occupied = 2,
    Reserved = 3,
    Cleaning = 4,
    OutOfService = 5
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Seated = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum KitchenOrderStatus
{
    New = 1,
    Acknowledged = 2,
    Preparing = 3,
    Ready = 4,
    Served = 5,
    Cancelled = 6
}

public enum ProductType
{
    Simple = 1,
    Variable = 2,
    Combo = 3,
    MealDeal = 4,
    Ingredient = 5,
    RawMaterial = 6
}

public enum StockMovementType
{
    Purchase = 1,
    Sale = 2,
    Adjustment = 3,
    Transfer = 4,
    Waste = 5,
    Return = 6,
    Production = 7,
    StockCount = 8
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum TaxType
{
    Percentage = 1,
    FixedAmount = 2,
    Inclusive = 3,
    Exclusive = 4
}

public enum InvoiceType
{
    Tax = 1,
    Simplified = 2
}

public enum ZatcaStatus
{
    Pending = 1,
    Reported = 2,
    Cleared = 3,
    Failed = 4,
    Skipped = 5
}

public enum ShiftStatus
{
    Open = 1,
    Closed = 2
}

public enum SupplierInvoiceStatus
{
    Draft = 1,
    Received = 2,
    Paid = 3,
    PartiallyPaid = 4,
    Cancelled = 5
}

public enum ExpenseStatus
{
    Draft = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4
}

public enum BranchType
{
    Restaurant = 1,
    Grocery = 2,
    Cafe = 3,
    Bakery = 4
}

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    Owner = 3,
    Manager = 4,
    Cashier = 5,
    Kitchen = 6,
    InventoryManager = 7,
    Accountant = 8,
    Auditor = 9,
    DeliveryDriver = 10,
    Waiter = 11
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum Language
{
    Arabic = 1,
    English = 2
}

public enum NotificationType
{
    LowStock = 1,
    KitchenReady = 2,
    NewOrder = 3,
    InvoiceFailed = 4,
    ShiftClosed = 5,
    ReservationReminder = 6
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

public enum WeightUnit
{
    Kilogram = 1,
    Gram = 2,
    Pound = 3,
    Ounce = 4,
    Liter = 5,
    Milliliter = 6
}

public enum LoyaltyTransactionType
{
    Earned = 1,
    Redeemed = 2,
    Expired = 3,
    Adjusted = 4
}
