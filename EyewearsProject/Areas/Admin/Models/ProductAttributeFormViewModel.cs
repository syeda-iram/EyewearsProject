using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductAttributeFormViewModel
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Value { get; set; } = "";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }
    }
}