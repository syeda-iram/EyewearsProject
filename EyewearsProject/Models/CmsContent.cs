namespace EyewearsProject.Models
{
    public class CmsContent
    {
        public int Id { get; set; }

        public CmsPageType Type { get; set; } = CmsPageType.Page;

        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";          // used in the URL, e.g. /Pages/about-us

        public string? Body { get; set; }                // rich text/html for Pages
        public string? ImageUrl { get; set; }             // used for Banners
        public string? LinkUrl { get; set; }               // where a Banner click goes

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }                 // controls banner/page display order

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}