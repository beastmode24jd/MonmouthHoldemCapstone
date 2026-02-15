using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    // Real authentication service that uses ASP.NET Core Identity
    // Replaces MockAuthenticationService with database-backed authentication
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthenticationService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
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


        // implement credential validation logic
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

        public async Task<bool> ResetPasswordAsync(string identifier, string newPassword)
        {
            // First validate the new password against the policy
            if (!IsPasswordValid(newPassword))
            {
                throw new ArgumentException(
                    "The given new password does not meet the policy standards and cannot be set.", nameof(newPassword));
            }

            // Find the user by email (identifier)
            var user = await _userManager.FindByEmailAsync(identifier);

            // If user doesn't exist, return false
            if (user == null)
            {
                return false;
            }

            // TODO - In the future, we should implement a proper password reset flow that involves sending a reset token
            // to the user's email. For now, we will generate a reset token and use it immediately to reset the password.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            return result.Succeeded;
        }
        
        public bool IsPasswordValid(string password)
        {
           
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;

            var hasLetter = password.Any(char.IsLetter);
            var hasDigit = password.Any(char.IsDigit);
            var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasLetter && hasDigit && hasSymbol;
        }

        public async Task<bool> DeactivateAccountAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var result = await _signInManager
                .CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded)
            {
                return false;
            }

            user.IsDeactivated = true;
            user.UserName = "Deactivated User";
            // Since we changed the UserName, we need to update the normalized username as well
            // so that the user cannot be found by their old username after deactivation
            await _userManager.UpdateNormalizedUserNameAsync(user);
            // Save the changes to the database. UserManager is our repo for Identity, so we use it to update the user.
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Account {Email} deactivated", email);
            return true;
        }


        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task UpdateUserProfileImageAsync(string email, byte[] pictureData, string contentType)
        {
            var user = await GetUserByEmailAsync(email);
            if (user != null)
            {
                user.ProfileImage = pictureData;
                user.ProfileImageType = contentType;
                // Update the user in the database. UserManager is our repo for Identity
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Updated profile image for {Email}.", email);
            }
            else
            {
                _logger.LogInformation("User with {Email} email not found.", email);
            }
        }


        // implement sign in logic
        public async Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                throw new InvalidOperationException($"User with email {email} not found");
            }

            // Sign in the user with cookie authentication
            // isPersistent = rememberMe creates a persistent cookie across browser sessions
            await _signInManager.SignInAsync(user, isPersistent: rememberMe);
        }

        public async Task SignOutUserAsync(HttpContext httpContext)
        {
            // sign out the user and clear authentication cookie
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            // check if a user with the given email exists in the database asynchronously
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }
    }
}