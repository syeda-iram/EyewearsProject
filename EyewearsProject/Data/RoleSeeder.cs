using EyewearsProject.Models;
using Microsoft.AspNetCore.Identity;

namespace EyewearsProject.Data
{
    public static class RoleSeeder
    {

        private static readonly string[] Roles =
        {
            "SuperAdmin",
            "Admin",
            "OrderManager",
            "ProductManager",
            "VendorManager",
            "FinanceManager",
            "MarketingManager",
            "CustomerSupport",
            "Vendor"
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var roleName in Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                }
            }

            const string superAdminEmail = "superadmin@eyewear.com";
            var existingAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (existingAdmin == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Super Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(superAdmin, "SuperAdmin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }
        }
    }
}