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
	
	    Task<bool> CheckPasswordAsync(string email, string password); /// Check Password

        Task<bool> DeactivateAccountAsync(string email);

	    Task<bool> ReactivateAccountAsync(string email, string password);

        Task<bool> RegisterUserAsync(string email, string password, string displayName = "UNSET");

        Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe);

        Task SignOutUserAsync(HttpContext httpContext);

        Task<bool> ResetPasswordAsync(string identifier, string newPassword);

        Task<bool> ResetPasswordWithTokenAsync(string email, string token, string newPassword);

        Task<string?> GenerateEmailConfirmationTokenAsync(string email);

        Task<bool> ConfirmEmailAsync(string email, string token);

        bool IsPasswordValid(string password);
    }
}
