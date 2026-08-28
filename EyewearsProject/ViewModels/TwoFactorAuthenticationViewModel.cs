using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class TwoFactorAuthenticationViewModel
    {
       public bool TwoFactorEnabled { get; set; }
       public bool EmailAvailable { get; set; }
        public bool PhoneAvailable { get; set; }
        [Required]
        public string CurrentMethod { get; set; } = "Email";
        public string SelectedMethod { get; set; } = "Email";
        public string? MaskedEmail { get; set; }
        public string? MaskedPhone { get; set; }
        [Display(Name = "Verification Code")]
        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "Verification code must be 6 digits.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Verification code must contain exactly 6 digits.")]
        public string? Otp { get; set; }
        public int OtpExpirySeconds { get; set; }
        public bool OtpSent { get; set; }
        public bool OtpVerified { get; set; }
        public string? Message { get; set; }
    }
}