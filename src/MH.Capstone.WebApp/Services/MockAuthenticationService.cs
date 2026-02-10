using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


// This is a temporary mock service that will be replaced with ASP.NET Core Identity in the future. 
// Once the database is set up, This is just to get the UI and testing working now.
namespace MH.Capstone.WebApp.Services
{

    // Mock authentication service for testing purposes. In a real application, this would be replaced with a service that interacts with a database or an external authentication provider.

    public interface IAuthenticationService
    {
        // Validates user credentials. In a real implementation, this would check the credentials against a database.
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<bool> RegisterUserAsync(string email, string password);
        Task SignInUserAsync(HttpContext httpContext, string email, bool rememberMe);
        Task SignOutUserAsync(HttpContext httpContext);
        bool UserExists(string email);
    }

    public class MockAuthenticationService : IAuthenticationService
    {
        // create a list of static users for testing purposes. In a real application, this data would come from a database.
        private static readonly List<(string Email, string Password)> _users = new()
        {
            ("test@example.com", "Test@123"),
            ("admin@example.com", "Admin@123")
        };

        // Logger used to record authentication events
        private readonly ILogger<MockAuthenticationService> _logger;

        // injecting the logger dependency through the constructor
        public MockAuthenticationService(ILogger<MockAuthenticationService> logger)
        {
            _logger = logger;
        }

        // Search our in-memory list for a matching user
        // FirstOrDefault returns the first match, or default (null) if none found
         public Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = _users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && 
                u.Password == password);

            if (user.Email != null)
            {
                _logger.LogInformation("User {Email} validated successfully", email);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Failed login attempt for {Email}", email);
            return Task.FromResult(false);
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
            _users.Add((email, password));
            _logger.LogInformation("User {Email} registered successfully", email);
            return Task.FromResult(true);
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

        public bool UserExists(string email)
        {
            return _users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
    }
}