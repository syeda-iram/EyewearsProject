using Microsoft.AspNetCore.Identity;

namespace EyewearsProject.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
    }
}