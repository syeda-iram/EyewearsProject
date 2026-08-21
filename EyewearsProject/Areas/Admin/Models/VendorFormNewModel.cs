using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class VendorFormViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Company Name")]
        public string CompanyName { get; set; } = "";

        [Required, Display(Name = "Contact Name")]
        public string ContactName { get; set; } = "";

        [Required, EmailAddress, Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = "";

        [Required, Phone, Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = "";

        public string? Address { get; set; }

        [Required, Display(Name = "Vendor Type")]
        public string VendorType { get; set; } = "Supplier";

        // Only used when creating — sets up the vendor's login account
        [Display(Name = "Login Password")]
        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
    }
}