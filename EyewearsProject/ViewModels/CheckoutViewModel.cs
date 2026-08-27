using System.ComponentModel.DataAnnotations;
using EyewearsProject.Models;

namespace EyewearsProject.ViewModels
{
    public class CheckoutViewModel
    {
        // =========================================================
        // ORDER SUMMARY
        // =========================================================

        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal { get; set; }

        public decimal ShippingAmount { get; set; } = 200;

        public decimal GrandTotal { get; set; }


        // =========================================================
        // CONTACT DETAILS
        // =========================================================

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = "";


        // =========================================================
        // SHIPPING ADDRESS
        // =========================================================

        // ID of the saved address selected by the customer.
        // Null means customer is entering the address manually.
        public int? ShippingAddressId { get; set; }

        [Required]
        [Display(Name = "Address Line 1")]
        public string ShippingAddressLine { get; set; } = "";

        [Display(Name = "Address Line 2")]
        public string? ShippingAddressLine2 { get; set; }

        [Required]
        [Display(Name = "City")]
        public string ShippingCity { get; set; } = "";

        [Display(Name = "State / Province")]
        public string? ShippingState { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string ShippingPostalCode { get; set; } = "";

        [Required]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; } = "Pakistan";


        // =========================================================
        // BILLING ADDRESS
        // =========================================================

        [Display(Name = "Billing address same as shipping")]
        public bool BillingSameAsShipping { get; set; } = true;

        // ID of saved billing address, if customer selects one.
        public int? BillingAddressId { get; set; }

        [Display(Name = "Billing Address Line 1")]
        public string? BillingAddressLine { get; set; }

        [Display(Name = "Billing Address Line 2")]
        public string? BillingAddressLine2 { get; set; }

        [Display(Name = "Billing City")]
        public string? BillingCity { get; set; }

        [Display(Name = "Billing State / Province")]
        public string? BillingState { get; set; }

        [Display(Name = "Billing Postal Code")]
        public string? BillingPostalCode { get; set; }

        [Display(Name = "Billing Country")]
        public string? BillingCountry { get; set; }


        // =========================================================
        // PAYMENT
        // =========================================================

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";


        // =========================================================
        // COUPON
        // =========================================================

        [Display(Name = "Coupon Code")]
        public string? CouponCode { get; set; }

        public decimal DiscountAmount { get; set; }
    }
}