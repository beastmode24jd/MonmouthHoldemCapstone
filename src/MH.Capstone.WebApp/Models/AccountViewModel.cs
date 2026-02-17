using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class AccountViewModel
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string ProfileImageUrl { get; }

        public AccountViewModel(ApplicationUser user)
        {
            Id = Guid.Parse(user.Id);
            Name = user.UserName;
            Email = user.Email;
            ProfileImageUrl = user.GetProfileImageUrl();
        } 
    }
}
