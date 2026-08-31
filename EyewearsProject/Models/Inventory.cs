namespace EyewearsProject.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public ProductVariant ProductVariant { get; set; } = null!;

        public int QuantityOnHand { get; set; }

        public int ReservedQuantity { get; set; }

        public int AvailableQuantity =>
            Math.Max(0, QuantityOnHand - ReservedQuantity);

        public int ReorderLevel { get; set; } = 10;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}