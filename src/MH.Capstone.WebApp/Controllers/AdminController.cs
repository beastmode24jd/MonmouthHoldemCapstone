using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthenticationService _authService;

        public AdminController(UserManager<ApplicationUser> userManager, IAuthenticationService authService)
        {
            _userManager = userManager;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Manage()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string email, string adminPassword)
        {
            // Check that the admin password is correct.
            if (!await VerifyAdminPasswordAsync(adminPassword))
            {
                TempData["Error"] = "Invalid administrator credentials.";
                return RedirectToAction(nameof(Manage));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Please provide a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            // Check if an admin is trying to redundantly promote themselves
            if (email.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You are already an Admin.";
                return RedirectToAction(nameof(Manage));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Generic error if user is not found
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            // Ensure that an account only has a single role at a time (User, Admin)
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove their current role(s), then default it to just the "Admin" role.
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                TempData["Success"] = $"User {email} has been promoted to Admin.";
            }
            else
            {
                TempData["Error"] = "Could not promote user to Admin role.";
            }

            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromAdmin(string email, string adminPassword)
        {
            // Check that the admin password is correct
            if (!await VerifyAdminPasswordAsync(adminPassword))
            {
                TempData["Error"] = "Invalid administrator credentials.";
                return RedirectToAction(nameof(Manage));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            // Prevent an Admin from demoting themselves,
            //          and crashing the page connection as a result
            if (email.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Security Check: You cannot revoke your own Admin rights.";
                return RedirectToAction(nameof(Manage));
            }

            var user = await _userManager.FindByEmailAsync(email);

            // Catches if the account exists as a User, or doesn't exist at all
            // Same error message is provided either way
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            // If the clause above passes, account is an Admin
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var result = await _userManager.AddToRoleAsync(user, "User");

            if (result.Succeeded)
            {
                TempData["Success"] = $"User {email} is now a standard User.";
            }
            else
            {
                TempData["Error"] = "Please enter a valid email address.";
            }

            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string targetEmail, string adminPassword)
        {
            // Front-end should catch this, but leaving this guard in
            if (string.IsNullOrWhiteSpace(targetEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                TempData["Error"] = "Both target email and your admin password are required.";
                return RedirectToAction(nameof(Manage));
            }

            // Verify the Admin's identity and password
            if (!await VerifyAdminPasswordAsync(adminPassword))
            {
                TempData["Error"] = "Invalid administrator credentials.";
                return RedirectToAction(nameof(Manage));
            }

            // VerifyAdminPasswordAsync checks if the adminUser is null, hence the bang operator
            var adminUser = await _userManager.GetUserAsync(User);
            if (targetEmail.Equals(adminUser!.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Prevent the Admin from deactivating themselves
                TempData["Error"] = "You cannot deactivate your account from the Admin Management page.";
                return RedirectToAction(nameof(Manage));
            }

            // Find the selected User account
            var targetUser = await _userManager.FindByEmailAsync(targetEmail);
            if (targetUser == null || targetUser.Email == null)
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            // Prevent the Admin from deleting *other* Admins.
            if (await _userManager.IsInRoleAsync(targetUser, "Admin"))
            {
                TempData["Error"] = "Security Restriction: You cannot deactivate another Administrator.";
                return RedirectToAction(nameof(Manage));
            }

            // Use AuthenticationService to perform the deactivation
            // targetUser has already been verified to not be null.
            var success = await _authService.DeactivateAccountAsync(targetUser.Email!);

            if (success)
            {
                TempData["Success"] = $"Account {targetUser.Email} has been successfully deactivated.";
            }
            else
            {
                TempData["Error"] = "User not found or operation failed.";
            }

            return RedirectToAction(nameof(Manage));
        }
        private async Task<bool> VerifyAdminPasswordAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            
            var adminUser = await _userManager.GetUserAsync(User);
            return adminUser != null && await _userManager.CheckPasswordAsync(adminUser, password);
        }
    }
}