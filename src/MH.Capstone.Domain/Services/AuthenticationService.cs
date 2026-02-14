using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MH.Capstone.Domain.Services
{
    // Real authentication service that uses ASP.NET Core Identity
    // Replaces MockAuthenticationService with database-backed authentication
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<bool> RegisterUserAsync(string email, string password)
        {
            // Implement registration logic
            // Creating a new ApplicationUser with the provided email
            var user = new ApplicationUser
            {
                UserName = email, //Identity requires UserName
                Email = email
            };

            // Use UserManager to create the user with the hashed password
            var result = await _userManager.CreateAsync(user, password);

            // if ture return successful, otherwise return false
            return result.Succeeded;
        }


        // implement crediential validation logic
        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(email);
            
            // If user doesn't exist, return false
            if (user == null)
            {
                return false;
            }

            // Check if the password is correct using SignInManager
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            // Return true if password is correct
            return result.Succeeded;
        }

        public Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe)
        {
            throw new NotImplementedException();
        }

        public Task SignOutUserAsync(HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public bool UserExists(string email)
        {
            throw new NotImplementedException();
        }
    }
}