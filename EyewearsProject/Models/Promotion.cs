namespace EyewearsProject.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string? Description { get; set; }

        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public decimal DiscountValue { get; set; }

        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }   // caps a percentage discount

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public int? UsageLimit { get; set; }               // null = unlimited
        public int UsageCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}