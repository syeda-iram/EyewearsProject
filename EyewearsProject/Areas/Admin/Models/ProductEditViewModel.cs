using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductEditViewModel
    {
        public int Id { get; set; }

        // =====================================================
        // BASIC PRODUCT INFORMATION
        // =====================================================

        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = "";

        // Product-level SKU
        [Required]
        public string Sku { get; set; } = "";

        public string? Description { get; set; }


        // =====================================================
        // CATEGORY / SUBCATEGORY / BRAND
        // =====================================================

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Subcategory")]
        public int? SubCategoryId { get; set; }

        [Display(Name = "Brand")]
        public int? BrandId { get; set; }


        // =====================================================
        // PRODUCT DETAILS
        // =====================================================

        public string? Gender { get; set; }

        public string? Material { get; set; }

        public string? Shape { get; set; }


        // =====================================================
        // PRICING
        // =====================================================

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Display(Name = "Discount Price")]
        [Range(0, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }

        [Display(Name = "Cost Price")]
        [Range(0, double.MaxValue)]
        public decimal? CostPrice { get; set; }

        [Display(Name = "Weight")]
        [Range(0, double.MaxValue)]
        public decimal? Weight { get; set; }


        // =====================================================
        // PRODUCT SETTINGS
        // =====================================================

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; }


        // =====================================================
        // VIRTUAL TRY-ON
        // =====================================================

        [Display(Name = "2D Overlay Image URL")]
        public string? TryOnOverlayImageUrl { get; set; }

        [Display(Name = "3D Try-On Model (.glb path)")]
        public string? TryOn3DModelUrl { get; set; }

        [Display(Name = "2D Overlay Scale")]
        public double TryOnOverlayScale { get; set; } = 1.0;

        [Display(Name = "2D Overlay Vertical Offset")]
        public double TryOnOverlayVerticalOffset { get; set; } = 0;


        // =====================================================
        // VARIANTS + INVENTORY
        //
        // Color and Size are variant-level options.
        // SKU is also maintained at variant level.
        // Product itself has its own SKU as well.
        // =====================================================

        public List<ProductEditVariantViewModel> Variants { get; set; }
            = new();


        // =====================================================
        // IMAGES
        // =====================================================

        public List<ProductEditImageViewModel> ExistingImages { get; set; }
            = new();

        public List<IFormFile> NewImages { get; set; }
            = new();


        // =====================================================
        // SPECIFICATIONS
        //
        // General product characteristics/information.
        // =====================================================

        public List<ProductEditSpecificationViewModel> Specifications { get; set; }
            = new();


        // =====================================================
        // ATTRIBUTES
        //
        // Dynamic measurements/details.
        //
        // Examples:
        // Frame Length
        // Bridge Width
        // Temple Length
        // Lens Width
        // Lens Height
        // etc.
        // =====================================================

        public List<ProductAttributeFormViewModel> Attributes { get; set; }
            = new();


        // =====================================================
        // TAGS
        // =====================================================

        public List<string> Tags { get; set; } = new();
    }
}