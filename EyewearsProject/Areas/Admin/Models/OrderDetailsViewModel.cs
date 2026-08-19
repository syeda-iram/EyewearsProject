using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public string RecipientName { get; set; } = "";
        public string RecipientEmail { get; set; } = "";
        public string RecipientPhone { get; set; } = "";

        public string ShippingAddressLine { get; set; } = "";
        public string ShippingCity { get; set; } = "";
        public string ShippingPostalCode { get; set; } = "";
        public string ShippingCountry { get; set; } = "";

        public string BillingAddressLine { get; set; } = "";
        public string BillingCity { get; set; } = "";
        public string BillingPostalCode { get; set; } = "";
        public string BillingCountry { get; set; } = "";

        public string? PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}