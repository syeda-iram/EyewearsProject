namespace EyewearsProject.Models
{
    public class Vendor
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public string? Address { get; set; }
        public string VendorType { get; set; } = "Supplier"; // Supplier, Importer, Fitter

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    }
}