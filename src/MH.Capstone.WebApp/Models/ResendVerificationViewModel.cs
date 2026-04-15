using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models
{
    public class ResendVerificationViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>True after the form is submitted — triggers the "check your email" banner.</summary>
        public bool EmailSent { get; set; }
    }
}
