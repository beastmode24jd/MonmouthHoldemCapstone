using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class AccountViewModel
    {
        public Guid Id { get; set; }

        public string? Username { get; set; }

        public string? Email { get; set; }

        public bool IsDeactivated { get; set; } = false; // Default to false since the deactivation status is unknown.

        public string ProfileImageUrl { get; set; } = string.Empty; // Initialize to avoid null reference issues.

        public bool IsAuthenticatedUser { get; set; } = false; // Default to false since the current authenticated user is unknown.

        // For ASP.NET Core model binding
        public AccountViewModel() { }

        // Constructor to initialize the view model from an ApplicationUser
        public AccountViewModel(ApplicationUser user, bool isAuthedUser = false)
        {
            Id = Guid.Parse(user.Id);
            Username = user.UserName;
            Email = user.Email;
            ProfileImageUrl = user.GetProfileImageUrl();
            IsAuthenticatedUser = isAuthedUser;
            IsDeactivated = user.IsDeactivated;
        }
    }
}
