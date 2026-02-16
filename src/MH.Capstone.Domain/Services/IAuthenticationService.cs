using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services
{
    public interface IAuthenticationService
    {
        // Validates user credentials. In a real implementation, this would check the credentials against a database.
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<bool> RegisterUserAsync(string email, string password);
        Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe);
        Task SignOutUserAsync(HttpContext httpContext);
        Task<bool> UserExistsAsync(string identifier);
        Task<bool> ResetPasswordAsync(string identifier, string newPassword);
        bool IsPasswordValid(string password);
        ApplicationUser? GetUserByEmail(string email);
        void UpdateUserProfileImage(string email, byte[] pictureData);
    }
}
