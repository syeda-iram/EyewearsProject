namespace EyewearsProject.Models
{
    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public string ItemDescription { get; set; } = ""; // free text — vendor's own product/frame/lens name
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => UnitCost * Quantity;
    }
}