namespace EyewearsProject.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        // Product relationship
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Variant options
        public string Color { get; set; } = "";

        public string? Size { get; set; }

        // Each variant has its own SKU
        public string Sku { get; set; } = "";

        // Variant images
        public List<ProductImage> Images { get; set; } = new();

        // Stock is managed through Inventory
        public Inventory? Inventory { get; set; }
    }
}