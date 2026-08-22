namespace EyewearsProject.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }

        // Not stored as a column — computed on read from the two fields above
        public int AvailableQuantity => QuantityOnHand - ReservedQuantity;

        public int ReorderLevel { get; set; } = 10;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}