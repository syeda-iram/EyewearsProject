namespace EyewearsProject.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public InventoryTransactionType TransactionType { get; set; }

        // Always a positive number — direction is implied by TransactionType,
        // e.g. Sale/Damage/Transfer reduce stock, Purchase/Return/Adjustment(+) increase it.
        public int Quantity { get; set; }

        // What triggered this transaction, e.g. "Order", "PurchaseOrder", "ManualAdjustment"
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}