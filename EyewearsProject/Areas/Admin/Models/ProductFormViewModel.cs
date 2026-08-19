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
    }
}