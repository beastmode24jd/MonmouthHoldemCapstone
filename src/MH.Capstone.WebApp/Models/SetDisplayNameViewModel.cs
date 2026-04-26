using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models
{
    public class SetDisplayNameViewModel
    {
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9 '\-\.]{2,50}$",
            ErrorMessage = "Display name may only contain letters, numbers, spaces, hyphens, apostrophes, and periods.")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
