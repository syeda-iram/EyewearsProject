using System.ComponentModel.DataAnnotations;
using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class CmsContentFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public CmsPageType Type { get; set; } = CmsPageType.Page;

        [Required]
        public string Title { get; set; } = "";

        [Required, Display(Name = "URL Slug")]
        public string Slug { get; set; } = "";

        [Display(Name = "Body / Content")]
        public string? Body { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Link URL")]
        public string? LinkUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }
    }
}