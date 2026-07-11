using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Physical warehouse for storing inventory.</summary>
public class Warehouse : BaseEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
    public ICollection<StockItem> StockItems { get; set; } = new HashSet<StockItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new HashSet<StockMovement>();
}

/// <summary>Current stock level per product per warehouse.</summary>
public class StockItem : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal? AverageCost { get; set; }
    public DateTime? LastCountDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

/// <summary>Stock movement record (in/out) with FIFO support.</summary>
public class StockMovement : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Reference { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

/// <summary>Recipe (bill of materials) for a product.</summary>
public class RecipeIngredient : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid IngredientProductId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal WastePercentage { get; set; }

    public Product Product { get; set; } = null!;
    public Product IngredientProduct { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}

/// <summary>Purchase order to a supplier.</summary>
public class PurchaseOrder : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid BranchId { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new HashSet<PurchaseOrderItem>();
    public ICollection<SupplierInvoice> Invoices { get; set; } = new HashSet<SupplierInvoice>();
}

/// <summary>Individual line in a purchase order.</summary>
public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

/// <summary>Supplier invoice (accounts payable).</summary>
public class SupplierInvoice : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? SupplierInvoiceNumber { get; set; }
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<SupplierInvoiceItem> Items { get; set; } = new HashSet<SupplierInvoiceItem>();
}

/// <summary>Line item on a supplier invoice.</summary>
public class SupplierInvoiceItem : BaseEntity
{
    public Guid SupplierInvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalCost { get; set; }

    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

/// <summary>Stock transfer between warehouses/branches.</summary>
public class StockTransfer : BaseEntity
{
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Warehouse FromWarehouse { get; set; } = null!;
    public Warehouse ToWarehouse { get; set; } = null!;
    public ICollection<StockTransferItem> Items { get; set; } = new HashSet<StockTransferItem>();
}

/// <summary>Line item in a stock transfer.</summary>
public class StockTransferItem : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }

    public StockTransfer StockTransfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

/// <summary>Periodic stock count session.</summary>
public class StockCount : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockCountItem> Items { get; set; } = new HashSet<StockCountItem>();
}

/// <summary>Individual counted item in a stock count.</summary>
public class StockCountItem : BaseEntity
{
    public Guid StockCountId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance => CountedQuantity - SystemQuantity;

    public StockCount StockCount { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
