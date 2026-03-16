using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
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
        private readonly INotificationService _notificationService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _authRepo;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            INotificationService notificationService,
            ILogger<AuthenticationService> logger,
            IRepository<ApplicationUser, ApplicationDbContext> authRepo)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _notificationService = notificationService;
            _logger = logger;
            _authRepo = authRepo;
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

            if (!result.Succeeded)
            {
                // failed to register user (e.g. email already exists, password doesn't meet requirements, etc.)
                return false;
            }

            // Handle post-registration logic, such as sending a welcome notification
            await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId, "Welcome to WildlifeAID!",
                "Your account has been successfully created. It's now time to get wild and explore what the great outdoors has to offer!"));

            // Add the default User role to new accounts
            await _userManager.AddToRoleAsync(user, "User");

            return true;
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

            if (user.IsDeactivated)
            {
                _logger.LogWarning("Failed login attempt for {Email} - account deactivated", email);
                return false;
            }

            // Check if the password is correct using SignInManager
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            // Return true if password is correct
            return result.Succeeded;
        }

        /// <summary>
        /// Checks if the password is correct for the given email.
        /// Does NOT check if account is deactivated - use for security-sensitive flows
        /// where we need to verify password before revealing account status.
        /// </summary>
        public async Task<bool> CheckPasswordAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
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

            var hasLowercase = password.Any(char.IsLower);
            var hasUppercase = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);
            var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasLowercase && hasUppercase && hasDigit && hasSymbol;
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
            // Don't change UserName - it must remain unique in the database
            // The display name "Deactivated User" should be handled at the presentation layer
            // when checking user.IsDeactivated

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Account {Email} deactivated", email);
            return true;
        }

        public async Task<bool> ReactivateAccountAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            // User must be deactivated to reactivate
            if (!user.IsDeactivated) return false;

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded)
            {
                return false;
            }

            user.IsDeactivated = false;

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Account {Email} reactivated", email);
            return true;
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

            // Send a notification about successful login
            var now = DateTimeOffset.UtcNow;

            // Check if the user has a login streak first, and mention the point
            //      multipler applied to their account if so.
            if (user.IsStreakActive)
            {
                await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId, "Successful Login",
                    $"Your account recorded a successful login at {now.ToLocalTime().ToString("g")}. Wasn't you? Reset your password now!",
                    now));

                await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId, "Active Streak Multiplier",
                    $"You've logged in enough times to start a streak! A points multiplier of x1.5 has been applied to your account.",
                    now));
            }
            else
            {
                await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId, "Successful Login",
                    $"Your account recorded a successful login at {now.ToLocalTime().ToString("g")}. Wasn't you? Reset your password now!",
                    now));
            }
        }

        public async Task SignOutUserAsync(HttpContext httpContext)
        {
            // sign out the user and clear authentication cookie
            await _signInManager.SignOutAsync();
        }
    }
}