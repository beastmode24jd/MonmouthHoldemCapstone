using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string? Identifier { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match.")]
        public string? ConfirmNewPassword { get; set; }

        public bool ShowPasswordResetFields { get; set; }
    }
}
