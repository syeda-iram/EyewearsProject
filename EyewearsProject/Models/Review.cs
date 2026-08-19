namespace EyewearsProject.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public int Rating { get; set; }   // 1-5
        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = false;   // moderation gate before it shows publicly

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}