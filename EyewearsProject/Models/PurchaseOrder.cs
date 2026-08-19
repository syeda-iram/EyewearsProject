namespace EyewearsProject.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string PoNumber { get; set; } = "";

        public int VendorId { get; set; }
        public Vendor Vendor { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        public List<PurchaseOrderItem> Items { get; set; } = new();
        public Invoice? Invoice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}