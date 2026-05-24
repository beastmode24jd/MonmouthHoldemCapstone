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
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;

        public AdminController(UserManager<ApplicationUser> userManager, 
        IAuthenticationService authService,
        IReportService reportService,
        IUserService userService,
        IAuditService auditService)
        {
            _userManager = userManager;
            _authService = authService;
            _reportService = reportService;
            _userService = userService;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Manage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reports(ReportQueueViewModel vm)
        {
            // Get the user device's local timezone cookie, default timezone is PST
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            TimeZoneInfo userZone;
            try
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                // Fallback for Windows environment or invalid IANA IDs
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }

            // Auto-fill the DateFilter to the current timezone date/time if not already set
            var displayNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, userZone);

            // Only auto-fill if the user hasn't selected a date yet
            // Ensures the <input type="date"> shows today's date in their timezone
            vm.DateFilter ??= displayNow;

            // If the user provided a search name, find the corresponding ID
            string? reporterId = null;

            if (!string.IsNullOrWhiteSpace(vm.UserSearch))
            {
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.DisplayName == vm.UserSearch);
                
                // If found, use their ID; if not found, use a dummy ID to return 0 results
                reporterId = user?.Id ?? "ID_NOT_FOUND";
            }

            // Get the converted DateTimeOffset values for local display from ReportService.cs
            var (reports, totalCount) = await _reportService.SortReports(
                vm.SortBy, 
                vm.PageUrlFilter, 
                reporterId, // Shows as Display Name to front-end
                vm.DateFilter,
                vm.ShowResolved, 
                vm.CurrentPage, 
                vm.PageSize,
                userZone);

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

        // Page that displays the admin action audit logs.
        [HttpGet]
        [Route("/Audit-Logs")]
        public async Task<IActionResult> LogPage(AuditQueueViewModel vm)
        {
            (List<AuditLog> Audits, int TotalCount) result;

            // Get the user device's local timezone cookie, default timezone is PST
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            TimeZoneInfo userZone;
            try
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                // Fallback for Windows environment or invalid IANA IDs
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }

            // Auto-fill the DateFilter to the current timezone date/time if not already set
            // Pass the user's current local date to the view to use as a placeholder
            ViewBag.CurrentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userZone).ToString("yyyy-MM-dd");

            // Decide which service method to call based on inputs
            if (!string.IsNullOrWhiteSpace(vm.AdminSearch))
            {
                // Convert DisplayName to Guid
                var adminUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.DisplayName == vm.AdminSearch);

                // If the user isn't found, use Guid.Empty to return 0 results.
                Guid adminId = adminUser != null ? adminUser.GuidId : Guid.Empty;

                result = await _auditService.GetAuditsByAdminAsync(adminId, vm.CurrentPage, vm.PageSize);
            }
            else if (!string.IsNullOrWhiteSpace(vm.UserSearch))
            {
                var targetUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.DisplayName == vm.UserSearch);
        
                // Convert to Guid using the GuidId property
                Guid targetId = targetUser != null ? targetUser.GuidId : Guid.Empty;

                result = await _auditService.GetAuditsByUserAsync(targetId, vm.CurrentPage, vm.PageSize);
            }
            else if (vm.DateFilter.HasValue)
            {
                // Push to end of the day so it includes the selected date
                var adjustedDate = vm.DateFilter.Value.Date.AddDays(1).AddTicks(-1); 
                result = await _auditService.GetAuditsByDateAsync(adjustedDate, vm.CurrentPage, vm.PageSize);
            }
            else
            {
                // Default view, if no filters are applied
                result = await _auditService.GetPagedAuditsAsync(vm.CurrentPage, vm.PageSize);
            }

            vm.Audits = result.Audits;

            // Protect against divide-by-zero if PageSize is 0
            if (vm.PageSize > 0) 
            {
                vm.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)vm.PageSize);
            }

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

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, bool findLocked)
        {
            // Search all active (non-deactivated) users by DisplayName
            var users = await _userService.SearchUsersAsync(term);
            
            // Filter based on whether we want currently locked or currently open accounts
            var filtered = users
                .Where(u => u.AccountLocked == findLocked)
                .Select(u => new { u.Email, u.DisplayName });

            return Json(filtered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAccountLock(string targetEmail, string adminPassword, bool shouldLock)
        {
            if (!await VerifyAdminPasswordAsync(adminPassword))
            {
                TempData["Error"] = "Invalid administrator credentials.";
                return RedirectToAction(nameof(Manage));
            }

            var targetUser = await _userManager.FindByEmailAsync(targetEmail);
            if (targetUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Manage));
            }

            // Prevent self-locking
            var adminUser = await _userManager.GetUserAsync(User);
            if (targetEmail.Equals(adminUser?.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You cannot lock out your own account.";
                return RedirectToAction(nameof(Manage));
            }

            var result = await _userService.LockToggleAccountAsync(targetUser, shouldLock);

            if (result)
            {
                TempData["Success"] = $"Account for {targetUser.DisplayName} has been {(shouldLock ? "locked" : "unlocked")}.";
            }
            else
            {
                TempData["Error"] = "An error occurred while updating the account status.";
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