using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IAuthenticationService
    {
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<bool> DeactivateAccountAsync(string email, string password);
	Task<bool> ReactivateAccountAsync(string email, string password);
        Task<bool> RegisterUserAsync(string email, string password);
        Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe);
        Task SignOutUserAsync(HttpContext httpContext);
        Task<bool> UserExistsAsync(string identifier);
        Task<bool> ResetPasswordAsync(string identifier, string newPassword);
        bool IsPasswordValid(string password);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task UpdateUserProfileImageAsync(string email, byte[] pictureData, string contentType);
    }
}
