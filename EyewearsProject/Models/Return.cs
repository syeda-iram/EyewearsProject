namespace EyewearsProject.Models
{
    public class Return
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public DateTime ReturnRequestDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = "";
        public ReturnStatus Status { get; set; } = ReturnStatus.Requested;
        public decimal RefundAmount { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}