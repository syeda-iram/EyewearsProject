using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}