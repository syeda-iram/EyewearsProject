using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "Profile Image URL")]
        public string? ProfileImage { get; set; }
    }
}