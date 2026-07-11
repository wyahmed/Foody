using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Core product entity supporting simple, variable, combo, and meal deal products.</summary>
public class Product : BaseEntity
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxRateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Simple;
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsWeightBased { get; set; }
    public bool HasExpiry { get; set; }
    public bool TrackInventory { get; set; } = true;
    public decimal? MinStockLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAvailableForDineIn { get; set; } = true;
    public bool IsAvailableForTakeAway { get; set; } = true;
    public bool IsAvailableForDelivery { get; set; } = true;
    public bool IsOpenPrice { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? NotesAr { get; set; }
    public string? KitchenNote { get; set; }
    public string? PrinterName { get; set; }
    public decimal? CaloriesPerServing { get; set; }
    public int PrepTimeMinutes { get; set; }

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? Unit { get; set; }
    public TaxRate? TaxRate { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new HashSet<ProductVariant>();
    public ICollection<ProductModifierGroup> ModifierGroups { get; set; } = new HashSet<ProductModifierGroup>();
    public ICollection<ProductPrice> Prices { get; set; } = new HashSet<ProductPrice>();
    public ICollection<ComboItem> ComboItems { get; set; } = new HashSet<ComboItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new HashSet<RecipeIngredient>();
    public ICollection<BranchProduct> BranchProducts { get; set; } = new HashSet<BranchProduct>();
    public ICollection<HappyHourProduct> HappyHourProducts { get; set; } = new HashSet<HappyHourProduct>();
}

/// <summary>Product variant (size, color, etc.).</summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public decimal AdditionalPrice { get; set; }
    public decimal CostPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}

/// <summary>Group of modifiers (e.g., "Extras", "Cooking Preference").</summary>
public class ModifierGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public bool IsMultiSelect { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Modifier> Modifiers { get; set; } = new HashSet<Modifier>();
    public ICollection<ProductModifierGroup> ProductModifierGroups { get; set; } = new HashSet<ProductModifierGroup>();
}

/// <summary>Individual modifier option (e.g., "Extra Cheese").</summary>
public class Modifier : BaseEntity
{
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ModifierGroup ModifierGroup { get; set; } = null!;
}

/// <summary>Junction between product and modifier group.</summary>
public class ProductModifierGroup : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid ModifierGroupId { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ModifierGroup ModifierGroup { get; set; } = null!;
}

/// <summary>Multiple price tiers (e.g., dine-in, take-away, delivery, wholesale).</summary>
public class ProductPrice : BaseEntity
{
    public Guid ProductId { get; set; }
    public string PriceLevel { get; set; } = "Default";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
}

/// <summary>Items included in a combo or meal deal.</summary>
public class ComboItem : BaseEntity
{
    public Guid ComboProductId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; }

    public Product ComboProduct { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Branch-specific product settings (price override, active status).</summary>
public class BranchProduct : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOutOfStock { get; set; }

    public Branch Branch { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
