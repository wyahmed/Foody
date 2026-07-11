using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Identity;
using System.Reflection;

namespace RestaurantPOS.Infrastructure.Data;

/// <summary>
/// Main application DbContext. Inherits from IdentityDbContext for ASP.NET Identity support.
/// Applies global query filters for soft delete and multi-tenancy automatically.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // --- Tenant / Branch ---
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();

    // --- Identity Extensions ---
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();

    // --- Catalog ---
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    // --- Products ---
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<BranchProduct> BranchProducts => Set<BranchProduct>();
    public DbSet<HappyHour> HappyHours => Set<HappyHour>();
    public DbSet<HappyHourProduct> HappyHourProducts => Set<HappyHourProduct>();

    // --- Tables & Reservations ---
    public DbSet<TableSection> TableSections => Set<TableSection>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<TableReservation> TableReservations => Set<TableReservation>();

    // --- Orders ---
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    // --- Kitchen ---
    public DbSet<KitchenOrder> KitchenOrders => Set<KitchenOrder>();

    // --- CRM ---
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // --- Inventory ---
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceItem> SupplierInvoiceItems => Set<SupplierInvoiceItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountItem> StockCountItems => Set<StockCountItem>();

    // --- Finance ---
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CashDrawerTransaction> CashDrawerTransactions => Set<CashDrawerTransaction>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // --- Settings / Misc ---
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from the current assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global soft-delete query filter for all BaseEntity types
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(Domain.Common.BaseEntity).IsAssignableFrom(clrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(clrType);
                method.Invoke(null, [builder]);
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder builder) where T : Domain.Common.BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserName ?? "system";
        var tenantId = _currentUserService.TenantId;

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedDate = now;
                    if (entry.Entity.TenantId == Guid.Empty && tenantId.HasValue)
                        entry.Entity.TenantId = tenantId.Value;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = userId;
                    entry.Entity.UpdatedDate = now;
                    break;

                case EntityState.Deleted:
                    // Convert hard delete to soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = now;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
