namespace EyewearsProject.Areas.Admin.Models
{
    public class InventoryItemViewModel
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string Color { get; set; } = "";
        public string? Size { get; set; }
        public int StockQuantity { get; set; }
    }
}