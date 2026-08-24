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
        public string CategoryName { get; set; } = "";
        public string BrandName { get; set; } = "";
        public decimal UnitPrice { get; set; }

        public decimal StockValue =>
            QuantityOnHand * UnitPrice;

        public string StockStatus
        {
            get
            {
                if (AvailableQuantity <= 0)
                    return "Out of Stock";

                if (AvailableQuantity <= ReorderLevel)
                    return "Low Stock";

                return "In Stock";
            }
        }
    }
}