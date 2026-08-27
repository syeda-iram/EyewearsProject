using EyewearsProject.Models;

namespace EyewearsProject.Data
{
    public static class DbInitializer
    {
        // Categories, Brands, and Products are now created entirely through the Admin portal
        // (/Admin/Categories, /Admin/Brands, /Admin/Products) instead of being hardcoded here.
        // This method is kept as a no-op placeholder in case future startup-time setup
        // (that genuinely shouldn't be admin-editable, e.g. system defaults) is ever needed.
        public static void Seed(AppDbContext context)
        {
            // Intentionally empty — no automatic Category/Brand/Product seeding.
        }
    }
}