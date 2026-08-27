namespace EyewearsProject.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? LogoUrl { get; set; }
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
