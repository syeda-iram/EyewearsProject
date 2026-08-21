using EyewearsProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Vendor
{
    public static class VendorContext
    {
        public static async Task<EyewearsProject.Models.Vendor?> GetCurrentVendorAsync(
            AppDbContext context, UserManager<ApplicationUser> userManager, System.Security.Claims.ClaimsPrincipal user)
        {
            var appUser = await userManager.GetUserAsync(user);
            if (appUser == null) return null;

            return await context.Vendors.FirstOrDefaultAsync(v => v.UserId == appUser.Id);
        }
    }
}