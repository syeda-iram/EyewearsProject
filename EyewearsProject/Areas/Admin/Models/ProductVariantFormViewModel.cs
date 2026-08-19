using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductVariantFormViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required]
        public string Color { get; set; } = "";

        public string? Size { get; set; }

        [Required]
        public string Sku { get; set; } = "";

        [Required, Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
    }
}