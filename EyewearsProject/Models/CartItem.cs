namespace EyewearsProject.Models
{
    public class CartItem
    {
        // Unique per cart line — needed because the same product/variant
        // can appear more than once with different lens configurations.
        public string LineId { get; set; } = Guid.NewGuid().ToString();

        public int ProductId { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string Color { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public string? LensType { get; set; }
        public string? Coating { get; set; }
        public int? PrescriptionId { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
    }
}