using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models.ViewModels
{
    public class DeactivateAccountViewModel
    {
        [Required(ErrorMessage = "Password is required to deactivate your account")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}