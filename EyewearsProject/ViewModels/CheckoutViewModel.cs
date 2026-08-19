using System.ComponentModel.DataAnnotations;
using EyewearsProject.Models;

namespace EyewearsProject.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal ShippingAmount { get; set; } = 200;
        public decimal GrandTotal { get; set; }

        // Contact
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, Phone, Display(Name = "Phone Number")]
        public string Phone { get; set; } = "";

        // Shipping address
        [Required, Display(Name = "Address")]
        public string ShippingAddressLine { get; set; } = "";

        [Required, Display(Name = "City")]
        public string ShippingCity { get; set; } = "";

        [Required, Display(Name = "Postal Code")]
        public string ShippingPostalCode { get; set; } = "";

        [Required, Display(Name = "Country")]
        public string ShippingCountry { get; set; } = "Pakistan";

        // Billing address
        [Display(Name = "Billing address same as shipping")]
        public bool BillingSameAsShipping { get; set; } = true;

        public string? BillingAddressLine { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }

        [Required, Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        [Display(Name = "Coupon Code")]
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}