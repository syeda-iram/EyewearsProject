using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductEditSpecificationViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Specification Name")]
        public string Name { get; set; } = "";

        [Required]
        [Display(Name = "Value")]
        public string Value { get; set; } = "";

        [Display(Name = "Sort Order")]
        [Range(0, int.MaxValue)]
        public int SortOrder { get; set; }
    }
}