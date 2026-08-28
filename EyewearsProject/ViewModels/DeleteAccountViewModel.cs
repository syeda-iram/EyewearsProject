using System.ComponentModel.DataAnnotations;

namespace EyewearsProject.ViewModels
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string Password { get; set; } = "";
    }
}
