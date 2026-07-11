using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Infrastructure.Identity;
using RestaurantPOS.Shared.Constants;

namespace RestaurantPOS.Infrastructure.Data.Seed;

/// <summary>
/// Seeds the database with initial data (tenant, admin user, roles, categories, products, etc.).
/// Safe to run on every startup - checks for existing records before inserting.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager, logger);
            var tenantId = await SeedTenantAsync(context, logger);
            var branchId = await SeedBranchAsync(context, tenantId, logger);
            await SeedAdminUserAsync(userManager, context, tenantId, branchId, logger);
            await SeedCatalogAsync(context, tenantId, logger);
            await SeedTablesAsync(context, branchId, logger);
            await SeedWarehouseAsync(context, branchId, logger);
            await SeedSettingsAsync(context, tenantId, branchId, logger);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach (var roleName in Roles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    IsSystemRole = true,
                    TenantId = Guid.Empty
                };
                await roleManager.CreateAsync(role);
                logger.LogInformation("Seeded role: {Role}", roleName);
            }
        }
    }

    private static async Task<Guid> SeedTenantAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Tenants.IgnoreQueryFilters().AnyAsync())
            return (await context.Tenants.IgnoreQueryFilters().FirstAsync()).Id;

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Demo Restaurant",
            NameAr = "مطعم تجريبي",
            VatNumber = "300000000000003",
            CommercialRegistration = "1234567890",
            Phone = "+966500000000",
            Email = "admin@demo.com",
            Address = "123 King Fahd Road",
            City = "Riyadh",
            Country = "SA",
            Currency = "SAR",
            DefaultLanguage = "ar",
            IsActive = true
        };
        tenant.TenantId = tenant.Id;

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default tenant.");
        return tenant.Id;
    }

    private static async Task<Guid> SeedBranchAsync(ApplicationDbContext context, Guid tenantId, ILogger logger)
    {
        if (await context.Branches.IgnoreQueryFilters().AnyAsync(b => b.TenantId == tenantId))
            return (await context.Branches.IgnoreQueryFilters().FirstAsync(b => b.TenantId == tenantId)).Id;

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentTenantId = tenantId,
            Name = "Main Branch",
            NameAr = "الفرع الرئيسي",
            Address = "123 King Fahd Road",
            City = "Riyadh",
            Phone = "+966500000001",
            BranchType = BranchType.Restaurant,
            IsActive = true,
            HasKitchenDisplay = true,
            DefaultTaxRate = 15,
            ServiceChargeRate = 0,
            Currency = "SAR",
            TimeZone = "Arab Standard Time"
        };

        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded main branch.");
        return branch.Id;
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        Guid tenantId,
        Guid branchId,
        ILogger logger)
    {
        const string adminEmail = "admin@restaurantpos.com";
        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            FirstNameAr = "المشرف",
            LastNameAr = "النظام",
            IsActive = true,
            PreferredLanguage = Language.Arabic
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
            logger.LogInformation("Seeded admin user: {Email}", adminEmail);
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context, Guid tenantId, ILogger logger)
    {
        if (await context.TaxRates.IgnoreQueryFilters().AnyAsync(t => t.TenantId == tenantId)) return;

        var vatRate = new TaxRate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "VAT 15%",
            NameAr = "ضريبة القيمة المضافة 15%",
            Rate = 15,
            IsDefault = true,
            IsActive = true,
            ZatcaCode = "S"
        };
        context.TaxRates.Add(vatRate);

        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Appetizers", NameAr = "المقبلات", SortOrder = 1, IsActive = true },
            new Category { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Main Course", NameAr = "الأطباق الرئيسية", SortOrder = 2, IsActive = true },
            new Category { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Beverages", NameAr = "المشروبات", SortOrder = 3, IsActive = true },
            new Category { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Desserts", NameAr = "الحلويات", SortOrder = 4, IsActive = true },
        };
        context.Categories.AddRange(categories);

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Piece",
            NameAr = "قطعة",
            Symbol = "pc",
            IsActive = true
        };
        context.Units.Add(unit);

        await context.SaveChangesAsync();

        // Sample products
        var mainCourse = categories[1];
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Grilled Chicken", NameAr = "دجاج مشوي", CategoryId = mainCourse.Id, UnitId = unit.Id, TaxRateId = vatRate.Id, SKU = "GRC001", CostPrice = 15, SellingPrice = 35, IsActive = true, TrackInventory = false },
            new Product { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Mixed Grill", NameAr = "مشاوي مشكلة", CategoryId = mainCourse.Id, UnitId = unit.Id, TaxRateId = vatRate.Id, SKU = "MGR001", CostPrice = 40, SellingPrice = 85, IsActive = true, TrackInventory = false },
            new Product { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Hummus", NameAr = "حمص", CategoryId = categories[0].Id, UnitId = unit.Id, TaxRateId = vatRate.Id, SKU = "HUM001", CostPrice = 3, SellingPrice = 12, IsActive = true, TrackInventory = false },
            new Product { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Water 500ml", NameAr = "ماء 500مل", CategoryId = categories[2].Id, UnitId = unit.Id, TaxRateId = vatRate.Id, SKU = "WAT001", Barcode = "5000112637922", CostPrice = 0.5m, SellingPrice = 3, IsActive = true, TrackInventory = true },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded catalog data.");
    }

    private static async Task SeedTablesAsync(ApplicationDbContext context, Guid branchId, ILogger logger)
    {
        if (await context.DiningTables.IgnoreQueryFilters().AnyAsync(t => t.BranchId == branchId)) return;

        var tenantId = (await context.Branches.IgnoreQueryFilters().FirstAsync(b => b.Id == branchId))!.TenantId;

        var section = new TableSection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Name = "Main Hall",
            NameAr = "القاعة الرئيسية",
            SortOrder = 1,
            IsActive = true
        };
        context.TableSections.Add(section);

        var tables = Enumerable.Range(1, 10).Select(i => new DiningTable
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            SectionId = section.Id,
            Name = $"T{i:D2}",
            Capacity = i % 3 == 0 ? 6 : 4,
            Status = TableStatus.Available,
            IsActive = true
        }).ToList();

        context.DiningTables.AddRange(tables);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} dining tables.", tables.Count);
    }

    private static async Task SeedWarehouseAsync(ApplicationDbContext context, Guid branchId, ILogger logger)
    {
        if (await context.Warehouses.IgnoreQueryFilters().AnyAsync(w => w.BranchId == branchId)) return;

        var tenantId = (await context.Branches.IgnoreQueryFilters().FirstAsync(b => b.Id == branchId))!.TenantId;

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Name = "Main Store",
            NameAr = "المخزن الرئيسي",
            IsDefault = true,
            IsActive = true
        };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default warehouse.");
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext context, Guid tenantId, Guid branchId, ILogger logger)
    {
        if (await context.Settings.IgnoreQueryFilters().AnyAsync(s => s.TenantId == tenantId)) return;

        var settings = new List<Setting>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "receipt.show_logo", Value = "true", Group = "Receipt", DataType = "bool" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "receipt.show_vat", Value = "true", Group = "Receipt", DataType = "bool" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "receipt.footer_text", Value = "Thank you for your visit", Group = "Receipt", DataType = "string" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "receipt.footer_text_ar", Value = "شكراً لزيارتكم", Group = "Receipt", DataType = "string" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "pos.default_order_type", Value = "DineIn", Group = "POS", DataType = "string" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "pos.require_table_for_dinein", Value = "true", Group = "POS", DataType = "bool" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "inventory.low_stock_threshold", Value = "10", Group = "Inventory", DataType = "int" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "zatca.enabled", Value = "false", Group = "ZATCA", DataType = "bool" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "loyalty.enabled", Value = "true", Group = "Loyalty", DataType = "bool" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = "loyalty.points_per_sar", Value = "1", Group = "Loyalty", DataType = "decimal" },
        };
        context.Settings.AddRange(settings);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded application settings.");
    }
}
