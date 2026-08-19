namespace EyewearsProject.Areas.Admin
{
    public static class AdminRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";

        // Full operational access — everything except managing SuperAdmin/Admin accounts
        public const string FullAccess = "SuperAdmin,Admin";

        // Everyone who can log into the Admin portal at all
        public const string AllAdmins =
            "SuperAdmin,Admin,ProductManager,OrderManager,MarketingManager,FinanceManager,CustomerSupport,VendorManager";

        // Only SuperAdmin and Admin may manage user accounts at all
        public const string UserManagers = "SuperAdmin,Admin";

        // Module-scoped roles (each includes FullAccess since SuperAdmin/Admin see everything)
        public const string ProductsModule = "SuperAdmin,Admin,ProductManager";
        public const string OrdersModule = "SuperAdmin,Admin,OrderManager";
        public const string MarketingModule = "SuperAdmin,Admin,MarketingManager";
        public const string FinanceModule = "SuperAdmin,Admin,FinanceManager";
        public const string SupportModule = "SuperAdmin,Admin,CustomerSupport";
        public const string VendorModule = "SuperAdmin,Admin,VendorManager";
    }
}