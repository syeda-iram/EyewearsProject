namespace EyewearsProject.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> Items { get; set; } = new();
        public Payment? Payment { get; set; }
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
    }
}