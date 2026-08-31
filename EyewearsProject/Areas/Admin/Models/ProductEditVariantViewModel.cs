using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductEditVariantViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Color { get; set; } = "";

        public string? Size { get; set; }

        [Required]
        public string Sku { get; set; } = "";

        [Range(0, int.MaxValue)]
        [Display(Name = "Stock")]
        public int StockQuantity { get; set; }

        public int ReservedQuantity { get; set; }

        public int AvailableQuantity =>
            Math.Max(0, StockQuantity - ReservedQuantity);
    }
}