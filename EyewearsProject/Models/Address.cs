using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EyewearsProject.Models
{
    public class Address
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        [ValidateNever]
        public ApplicationUser User { get; set; } = null!;

        public string Label { get; set; } = "Home";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string City { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Country { get; set; } = "Pakistan";

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}