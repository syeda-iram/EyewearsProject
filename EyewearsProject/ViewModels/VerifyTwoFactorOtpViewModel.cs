using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class VerifyTwoFactorOtpViewModel
    {
        [Required]
        public string Method { get; set; } = "Email";

        [Required(ErrorMessage = "Please enter the OTP.")]
        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "OTP must be exactly 6 digits.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "OTP must contain only 6 digits.")]
        [Display(Name = "OTP")]
        public string Otp { get; set; } = "";

        public int RemainingSeconds { get; set; }
    }
}