namespace EyewearsProject.Areas.Admin.Models
{
    public class InventoryItemViewModel
    {
        public int InventoryId { get; set; }
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string Color { get; set; } = "";
        public string? Size { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReorderLevel { get; set; }
    }
}