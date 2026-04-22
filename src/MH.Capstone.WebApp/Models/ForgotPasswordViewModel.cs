using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>True after the form is submitted — triggers the "check your email" message.</summary>
        public bool EmailSent { get; set; }
    }
}
