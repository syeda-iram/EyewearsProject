namespace EyewearsProject.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int BrandId { get; set; }
        public Brand Brand { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public string? TryOnOverlayImageUrl { get; set; } // transparent PNG for virtual try-on overlay
        public string? TryOn3DModelUrl { get; set; } // path to a .glb file for the full 3D try-on experience
        public List<ProductVariant> Variants { get; set; } = new();
        public List<ProductImage> Images { get; set; } = new();
        public List<ProductSpecification> Specifications { get; set; } = new();
        public double TryOnOverlayScale { get; set; } = 1.0;  // multiplier — >1 makes it bigger, <1 smaller
        public double TryOnOverlayVerticalOffset { get; set; } = 0; // positive moves down, negative moves up
    }
}
