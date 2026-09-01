namespace EyewearsProject.Models
{
    // One row per status change on an order. Lets the customer-facing
    // "Track Order" page show a real timeline instead of just the current status.
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Optional context, e.g. a cancellation reason.
        public string? Note { get; set; }
    }
}