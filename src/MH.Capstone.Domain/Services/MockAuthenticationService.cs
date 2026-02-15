using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MH.Capstone.Domain.DataModels;

// This is a temporary mock service that will be replaced with ASP.NET Core Identity in the future. 
// Once the database is set up, This is just to get the UI and testing working now.
namespace MH.Capstone.Domain.Services
{

    // Mock authentication service for testing purposes. In a real application, this would be replaced with a service that interacts with a database or an external authentication provider.

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
        void UpdateUserProfileImage(string email, string imageUrl);
    }

    public class MockAuthenticationService : IAuthenticationService
    {
        // create a list of static users for testing purposes. In a real application, this data would come from a database.
        private static readonly List<ApplicationUser> _users = new()
        {
            new ApplicationUser { Email = "test@example.com", Password = "Test@123" },
            new ApplicationUser { Email = "admin@example.com", Password = "Admin@123" }
        };

        // Logger used to record authentication events
        private readonly ILogger<MockAuthenticationService> _logger;

        // injecting the logger dependency through the constructor
        public MockAuthenticationService(ILogger<MockAuthenticationService> logger)
        {
            _logger = logger;
        }

        // Helper method to find a user by email
        public ApplicationUser? GetUserByEmail(string email) 
        {
            return _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateUserProfileImage(string email, string imageUrl)
        {
            var user = GetUserByEmail(email);
            if (user != null)
            {
                user.ProfileImageUrl = imageUrl;
                _logger.LogInformation("Updated profile image for {Email} to {Path}", email, imageUrl);
            }
        }

        // Search our in-memory list for a matching user
        // FirstOrDefault returns the first match, or default (null) if none found
         public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = GetUserByEmail(email);

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for {Email}", email);
            }
            else
            {
                _logger.LogInformation("User {Email} validated successfully", email);
            }

            return await Task.FromResult(user != null && user.Password == password);
        }

        // Registers a new user by adding them to our in-memory list.
        // In a real system, this would insert a new row into the database.
        public Task<bool> RegisterUserAsync(string email, string password)
        {
            if (UserExists(email))
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", email);
                return Task.FromResult(false);
            }
        // if user doesnt exist, add new user to in memory list
            _users.Add(new ApplicationUser { Email = email, Password = password });
            _logger.LogInformation("User {Email} registered successfully", email);
            return Task.FromResult(true);
        }
        public Task<bool> UserExistsAsync(string identifier)
        {
            identifier = (identifier ?? string.Empty).Trim();
            return Task.FromResult(_users.Any(u => u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<bool> ResetPasswordAsync(string identifier, string newPassword)
        {
            identifier = (identifier ?? string.Empty).Trim();

            var idx = _users.FindIndex(u => u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                return Task.FromResult(false);
            }

            _users[idx] = (_users[idx].Email, newPassword);
            _logger.LogInformation("Password reset for {Identifier}", identifier);

            return Task.FromResult(true);
        }
        
        public bool IsPasswordValid(string password)
        {
           
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;

            var hasLetter = password.Any(char.IsLetter);
            var hasDigit = password.Any(char.IsDigit);
            var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasLetter && hasDigit && hasSymbol;
        }

        // sign a user into the app by creating a claim (a piece of info about the user) and using cookie authentication to persist that claim across requests.
        public async Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(1)
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Email} signed in (RememberMe: {RememberMe})", email, rememberMe);
        }

        public async Task SignOutUserAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User signed out");
        }
    }
}