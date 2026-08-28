using Microsoft.AspNetCore.Identity;

namespace EyewearsProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImage { get; set; }

        public bool EmailVerified => EmailConfirmed;
        public bool PhoneVerified { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorMethod { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}