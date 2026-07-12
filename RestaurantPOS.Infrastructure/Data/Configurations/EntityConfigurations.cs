using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Infrastructure.Identity;

namespace RestaurantPOS.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(p => p.SKU).HasMaxLength(100);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.CostPrice).HasColumnType("decimal(18,4)");
        builder.Property(p => p.SellingPrice).HasColumnType("decimal(18,4)");
        builder.Property(p => p.MinStockLevel).HasColumnType("decimal(18,4)");
        builder.Property(p => p.MaxStockLevel).HasColumnType("decimal(18,4)");

        builder.HasIndex(p => new { p.TenantId, p.Barcode }).HasFilter("[Barcode] IS NOT NULL");
        builder.HasIndex(p => new { p.TenantId, p.SKU }).HasFilter("[SKU] IS NOT NULL");
        builder.HasIndex(p => new { p.TenantId, p.CategoryId });

        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Brand).WithMany(b => b.Products).HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Unit).WithMany(u => u.Products).HasForeignKey(p => p.UnitId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.TaxRate).WithMany(t => t.Products).HasForeignKey(p => p.TaxRateId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(o => o.SubTotal).HasColumnType("decimal(18,4)");
        builder.Property(o => o.DiscountAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.TaxAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.ServiceChargeAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.DeliveryChargeAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.TipsAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.PaidAmount).HasColumnType("decimal(18,4)");
        builder.Property(o => o.ChangeAmount).HasColumnType("decimal(18,4)");

        builder.HasIndex(o => new { o.BranchId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.BranchId, o.CreatedDate });
        builder.HasIndex(o => o.CustomerId);

        builder.HasOne(o => o.Branch).WithMany(b => b.Orders).HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.Table).WithMany(t => t.Orders).HasForeignKey(o => o.TableId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.Shift).WithMany(s => s.Orders).HasForeignKey(o => o.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(100).IsRequired();
        builder.Property(i => i.SubTotal).HasColumnType("decimal(18,4)");
        builder.Property(i => i.DiscountAmount).HasColumnType("decimal(18,4)");
        builder.Property(i => i.TaxableAmount).HasColumnType("decimal(18,4)");
        builder.Property(i => i.TaxAmount).HasColumnType("decimal(18,4)");
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(18,4)");

        builder.HasIndex(i => new { i.BranchId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => i.ZatcaStatus);

        builder.HasOne(i => i.Order).WithOne(o => o.Invoice).HasForeignKey<Invoice>(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(s => s.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(s => s.TotalCost).HasColumnType("decimal(18,4)");
        builder.Property(s => s.BalanceBefore).HasColumnType("decimal(18,4)");
        builder.Property(s => s.BalanceAfter).HasColumnType("decimal(18,4)");

        builder.HasIndex(s => new { s.WarehouseId, s.ProductId, s.MovementDate });
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.TotalSpent).HasColumnType("decimal(18,4)");
        builder.Property(c => c.StoreCredit).HasColumnType("decimal(18,4)");

        builder.HasIndex(c => new { c.TenantId, c.Phone }).HasFilter("[Phone] IS NOT NULL");
        builder.HasIndex(c => new { c.TenantId, c.Email }).HasFilter("[Email] IS NOT NULL");
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.EmployeeCode).HasMaxLength(50);

        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => u.BranchId);
    }
}

public class ComboItemConfiguration : IEntityTypeConfiguration<ComboItem>
{
    public void Configure(EntityTypeBuilder<ComboItem> builder)
    {
        builder.HasOne(c => c.ComboProduct)
            .WithMany(p => p.ComboItems)
            .HasForeignKey(c => c.ComboProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.HasOne(r => r.Product)
            .WithMany(p => p.RecipeIngredients)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.IngredientProduct)
            .WithMany()
            .HasForeignKey(r => r.IngredientProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.HasOne(s => s.FromWarehouse)
            .WithMany()
            .HasForeignKey(s => s.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ToWarehouse)
            .WithMany()
            .HasForeignKey(s => s.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
