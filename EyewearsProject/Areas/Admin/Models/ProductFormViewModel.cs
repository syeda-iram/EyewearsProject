using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Sku { get; set; } = "";

        public string? Description { get; set; }

        [Required, Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required, Display(Name = "Brand")]
        public int BrandId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Display(Name = "Discount Price")]
        public decimal? DiscountPrice { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Try-On Overlay Image URL")]
        public string? TryOnOverlayImageUrl { get; set; }

        [Display(Name = "3D Try-On Model (.glb path)")]
        public string? TryOn3DModelUrl { get; set; }

        [Display(Name = "2D Overlay Scale (default 1.0)")]
        public double TryOnOverlayScale { get; set; } = 1.0;

        [Display(Name = "2D Overlay Vertical Offset (default 0)")]
        public double TryOnOverlayVerticalOffset { get; set; } = 0;
    }
}