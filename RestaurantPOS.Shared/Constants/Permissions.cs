namespace RestaurantPOS.Shared.Constants;

/// <summary>Application-wide permission constants used in role-based authorization.</summary>
public static class Permissions
{
    public static class Branches
    {
        public const string View = "Branches.View";
        public const string Create = "Branches.Create";
        public const string Edit = "Branches.Edit";
        public const string Delete = "Branches.Delete";
    }

    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Edit = "Products.Edit";
        public const string Delete = "Products.Delete";
        public const string ManagePrices = "Products.ManagePrices";
    }

    public static class Orders
    {
        public const string View = "Orders.View";
        public const string Create = "Orders.Create";
        public const string Edit = "Orders.Edit";
        public const string Cancel = "Orders.Cancel";
        public const string Void = "Orders.Void";
        public const string ApplyDiscount = "Orders.ApplyDiscount";
        public const string Refund = "Orders.Refund";
    }

    public static class Tables
    {
        public const string View = "Tables.View";
        public const string Manage = "Tables.Manage";
        public const string Transfer = "Tables.Transfer";
        public const string Merge = "Tables.Merge";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Adjust = "Inventory.Adjust";
        public const string Transfer = "Inventory.Transfer";
        public const string StockCount = "Inventory.StockCount";
    }

    public static class Purchases
    {
        public const string View = "Purchases.View";
        public const string Create = "Purchases.Create";
        public const string Approve = "Purchases.Approve";
        public const string Receive = "Purchases.Receive";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Create = "Customers.Create";
        public const string Edit = "Customers.Edit";
        public const string Delete = "Customers.Delete";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Create = "Suppliers.Create";
        public const string Edit = "Suppliers.Edit";
        public const string Delete = "Suppliers.Delete";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
        public const string Export = "Reports.Export";
        public const string Financial = "Reports.Financial";
    }

    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Edit = "Settings.Edit";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManageRoles = "Users.ManageRoles";
    }

    public static class Shifts
    {
        public const string Open = "Shifts.Open";
        public const string Close = "Shifts.Close";
        public const string View = "Shifts.View";
    }

    public static class Expenses
    {
        public const string View = "Expenses.View";
        public const string Create = "Expenses.Create";
        public const string Approve = "Expenses.Approve";
    }

    public static class Kitchen
    {
        public const string View = "Kitchen.View";
        public const string ManageOrders = "Kitchen.ManageOrders";
    }

    /// <summary>Returns all permissions defined in this class for default role assignment.</summary>
    public static IEnumerable<string> GetAll()
    {
        return typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!);
    }
}

/// <summary>Predefined role names.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string Kitchen = "Kitchen";
    public const string InventoryManager = "InventoryManager";
    public const string Accountant = "Accountant";
    public const string Auditor = "Auditor";
    public const string Waiter = "Waiter";

    public static readonly string[] AllRoles = [SuperAdmin, Admin, Owner, Manager, Cashier, Kitchen, InventoryManager, Accountant, Auditor, Waiter];
}

/// <summary>Application-wide string constants.</summary>
public static class AppConstants
{
    public const string DefaultCurrency = "SAR";
    public const string DefaultLanguage = "ar";
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const string ZatcaDateFormat = "yyyy-MM-dd";
    public const string ZatcaDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
    public const string DefaultVatRate = "15";
    public const string CacheKeyPrefix = "RestaurantPOS:";
    public const int LowStockThreshold = 10;
}
