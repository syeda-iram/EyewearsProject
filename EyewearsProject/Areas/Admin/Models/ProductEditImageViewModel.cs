using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductEditImageViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = "";

        public bool IsPrimary { get; set; }

        public int SortOrder { get; set; }

        [Display(Name = "Color / Variant")]
        public int? ProductVariantId { get; set; }
    }
}