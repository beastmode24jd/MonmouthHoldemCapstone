using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MH.Capstone.WebApp.Models;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IAuthenticationService _authService;

        private readonly IReportService _reportService;

        public AdminController(UserManager<ApplicationUser> userManager, 
        IAuthenticationService authService,
        IReportService reportService)
        {
            _userManager = userManager;
            _authService = authService;
            _reportService = reportService;
        }

        [HttpGet]
        public IActionResult Manage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reports(ReportQueueViewModel vm)
        {
            // Auto-fill the DateFilter to the current UTC date/time if not already set [cite: 7]
            vm.DateFilter ??= DateTime.UtcNow;

            string? reporterId = null;

            // If the user provided a search name, find the corresponding ID
            if (!string.IsNullOrWhiteSpace(vm.UserSearch))
            {
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.DisplayName == vm.UserSearch);
                
                // If found, use their ID; if not found, use a dummy ID to return 0 results
                reporterId = user?.Id ?? "ID_NOT_FOUND";
            }

            var (reports, totalCount) = await _reportService.SortReports(
                vm.SortBy, 
                vm.PageUrlFilter, 
                reporterId, // Shows as Display Name to front-end
                vm.DateFilter, // Current UTC time as default (FOR NOW) if none is selected
                vm.ShowResolved, 
                vm.CurrentPage, 
                vm.PageSize);

            vm.Reports = reports;
            vm.TotalPages = (int)Math.Ceiling(totalCount / (double)vm.PageSize);

            // Populate the SelectList items here
            vm.SortOptions = Enum.GetValues(typeof(ReportFilterType))
                .Cast<ReportFilterType>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = e.ToString()
                }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Add the 'bool status' parameter to receive the checkbox state
        public async Task<IActionResult> UpdateResolution(Guid id, bool status) 
        {
            // Call the revised service method
            var success = await _reportService.SetReportResolution(id, status);
            
            if (success)
            {
                return Json(new { success = true });
            }
            
            return BadRequest(new { success = false, message = "Report not found." });
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