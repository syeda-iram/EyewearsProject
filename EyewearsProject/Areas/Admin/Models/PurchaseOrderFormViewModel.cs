using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class PurchaseOrderFormViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Vendor")]
        public int VendorId { get; set; }

        [Display(Name = "Expected Delivery")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public List<PurchaseOrderItemInput> Items { get; set; } = new() { new PurchaseOrderItemInput() };
    }

    public class PurchaseOrderItemInput
    {
        public int? ProductVariantId { get; set; }
        public string ItemDescription { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitCost { get; set; }
    }
}