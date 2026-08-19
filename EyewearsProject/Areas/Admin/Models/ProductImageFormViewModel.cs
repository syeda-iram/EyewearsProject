using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductImageFormViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required, Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = "";

        public bool IsPrimary { get; set; }

        [Display(Name = "Color / Variant")]
        public int? ProductVariantId { get; set; }
    }
}