using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EyewearsProject.Models
{
    public enum AddressType
    {
        Home,
        Office,
        Billing,
        Shipping
    }

    public class Address
    {
        public int Id { get; set; }

        // Identity user who owns this address
        [Required]
        public string UserId { get; set; } = "";

        [ValidateNever]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public AddressType AddressType { get; set; } = AddressType.Home;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [Phone]
        [StringLength(30)]
        public string Phone { get; set; } = "";

        [Required]
        [StringLength(250)]
        public string AddressLine1 { get; set; } = "";

        [StringLength(250)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = "";

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Pakistan";

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}