using EyewearsProject.Models;

namespace EyewearsProject.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (context.Products.Any()) return; // already seeded

            var sunglassesCategory = new Category { Name = "Sunglasses" };
            var readingCategory = new Category { Name = "Reading Glasses" };

            var rayban = new Brand { Name = "Ray-Ban" };
            var localBrand = new Brand { Name = "House Brand" };

            context.Categories.AddRange(sunglassesCategory, readingCategory);
            context.Brands.AddRange(rayban, localBrand);
            context.SaveChanges(); // save first so Ids are generated

            var product1 = new Product
            {
                Name = "Classic Aviator Sunglasses",
                Sku = "SUN-001",
                Description = "Polarized aviator sunglasses with UV protection.",
                CategoryId = sunglassesCategory.Id,
                BrandId = rayban.Id,
                Price = 4500,
                IsActive = true,
                Variants = new List<ProductVariant>
                {
                    new ProductVariant { Color = "Black", StockQuantity = 20, Sku = "SUN-001-BLK" },
                    new ProductVariant { Color = "Brown", StockQuantity = 15, Sku = "SUN-001-BRN" }
                },
                Images = new List<ProductImage>
                {
                    new ProductImage { ImageUrl = "/images/aviator-black.jpg", IsPrimary = true }
                }
            };

            var product2 = new Product
            {
                Name = "Budget Reading Glasses +2.0",
                Sku = "RDG-002",
                Description = "Lightweight reading glasses, +2.0 power.",
                CategoryId = readingCategory.Id,
                BrandId = localBrand.Id,
                Price = 800,
                IsActive = true,
                Variants = new List<ProductVariant>
                {
                    new ProductVariant { Color = "Black", StockQuantity = 50, Sku = "RDG-002-BLK" }
                },
                Images = new List<ProductImage>
                {
                    new ProductImage { ImageUrl = "/images/reading-black.jpg", IsPrimary = true }
                }
            };

            context.Products.AddRange(product1, product2);
            context.SaveChanges();

            // Seed matching Inventory rows so the new inventory system has real data from day one
            foreach (var variant in product1.Variants.Concat(product2.Variants))
            {
                context.Inventories.Add(new Inventory
                {
                    ProductVariantId = variant.Id,
                    QuantityOnHand = variant.StockQuantity,
                    ReservedQuantity = 0,
                    ReorderLevel = 10,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            context.SaveChanges();
        }
    }
}