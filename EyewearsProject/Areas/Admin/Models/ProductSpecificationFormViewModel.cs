using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ProductSpecificationFormViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Value { get; set; } = "";

        public int SortOrder { get; set; }
    }
}