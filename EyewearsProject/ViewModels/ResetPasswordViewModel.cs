using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}