namespace EyewearsProject.Models
{
    public class Product
    {
        public int Id { get; set; }

        // =====================================================
        // BASIC INFORMATION
        // =====================================================

        public string Name { get; set; } = "";

        public string Sku { get; set; } = "";

        public string? Description { get; set; }


        // =====================================================
        // CATEGORY / SUBCATEGORY / BRAND
        // =====================================================

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public int? SubCategoryId { get; set; }

        public Category? SubCategory { get; set; }

        public int? BrandId { get; set; }

        public Brand? Brand { get; set; }


        // =====================================================
        // PRODUCT DETAILS
        // =====================================================

        public string? Gender { get; set; }

        public string? Material { get; set; }

        public string? Shape { get; set; }


        // =====================================================
        // PRICING
        // =====================================================

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public decimal? CostPrice { get; set; }

        public decimal? Weight { get; set; }


        // =====================================================
        // STATUS
        // =====================================================

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;


        // =====================================================
        // VIRTUAL TRY-ON
        // =====================================================

        public string? TryOnOverlayImageUrl { get; set; }

        public string? TryOn3DModelUrl { get; set; }

        public double TryOnOverlayScale { get; set; } = 1.0;

        public double TryOnOverlayVerticalOffset { get; set; } = 0;


        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        public List<ProductVariant> Variants { get; set; } = new();

        public List<ProductImage> Images { get; set; } = new();

        public List<ProductSpecification> Specifications { get; set; } = new();

        public List<ProductAttribute> Attributes { get; set; } = new();

        public List<ProductTag> ProductTags { get; set; } = new();


        // =====================================================
        // AUDIT / TIMESTAMPS
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}