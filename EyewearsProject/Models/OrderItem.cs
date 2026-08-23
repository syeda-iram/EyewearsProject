namespace EyewearsProject.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public string ProductName { get; set; } = "";   // snapshot, in case product changes later
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;

        // Lens customization snapshot — kept even if the customer's saved
        // prescription is later edited or deleted, so fulfillment always
        // has an accurate record of what was ordered.
        public string? LensType { get; set; }
        public string? Coating { get; set; }
        public int? PrescriptionId { get; set; }
    }
}