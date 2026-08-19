using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.Areas.Admin.Models
{
    public class UserEditViewModel
    {
        public string Id { get; set; } = "";

        [Required]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        public bool IsActive { get; set; }

        [Display(Name = "Role")]
        public string SelectedRole { get; set; } = "";
    }
}