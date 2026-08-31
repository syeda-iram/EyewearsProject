using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Product being reviewed
        [Required]
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Customer who submitted the review
        [Required]
        public string UserId { get; set; } = "";

        public ApplicationUser User { get; set; } = null!;

        // Rating from 1 to 5
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        // Optional review text
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }

        // Review must be approved before appearing publicly
        public bool IsApproved { get; set; } = false;

        // Review timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}