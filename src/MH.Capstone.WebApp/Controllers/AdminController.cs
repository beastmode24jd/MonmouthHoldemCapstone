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

            ViewBag.UserTimeZone = userZone;

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

        [HttpGet]
        [Route("/Admin/SearchUserNames")]
        public async Task<IActionResult> SearchUserNames([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<string>());

            // Search the database for DisplayNames containing the typed letters
            var matches = await _userManager.Users
                .Where(u => u.DisplayName.Contains(term))
                .Select(u => u.DisplayName)
                .Take(10) // Limit to 10 suggestions to keep the dropdown clean
                .ToListAsync();

            return Json(matches);
        }
        
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Add the 'bool status' parameter to receive the checkbox state
        public async Task<IActionResult> UpdateResolution(Guid id, [FromQuery] bool status, [FromQuery] string? details) 
        {
            var report = await _reportService.GetReportByIdAsync(id);
    
            if (report == null)
            {
                return NotFound("Report not found.");
            }
            
            // Capture the original state BEFORE updating the database
            bool originalStatus = report.IsResolved;

            // Attempt to update the status in the Report DB
            bool updateSuccess = await _reportService.SetReportResolution(id, status);

            // Handle routing and Audit generation based on success
            if (updateSuccess)
            {
                // Only generate an Audit if the state ACTUALLY changed, 
                // OR if the admin explicitly typed a note/details to append to a resolved report.
                if (originalStatus != status || !string.IsNullOrWhiteSpace(details))
                {
                    // Grab the Admin who successfully made the change
                    var adminUser = await _userManager.GetUserAsync(User);
                    
                    if (adminUser != null)
                    {
                        // Construct the Audit Log
                        var audit = new AuditLog
                        {
                            // Assign enum based on checkbox status
                            ActionType = status ? AuditActionType.ReportResolved : AuditActionType.ReportOpened,
                            
                            PerformingUserId = adminUser.GuidId,
                            TargetReportId = id,
                            
                            // Directly link the audit to the user who submitted the report
                            TargetUserId = report.ReportingUserId,
                            
                            // Only assign the Details string if the Admin actually typed something
                            Details = string.IsNullOrWhiteSpace(details) ? null : details,
                            Timestamp = DateTimeOffset.UtcNow
                        };

                        // Save the audit to the database
                        await _auditService.LogActionAsync(audit);
                    }
                }
                // Return a 200 OK to the AJAX fetch call so it can trigger location.reload()
                return Ok(); 
            }
            else
            {
                // If the database update failed, return a 400 Bad Request to trigger the AJAX error alert
                return BadRequest("Failed to update report status.");
            }
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
        [Route("/Admin/SearchUsers")]
        public async Task<IActionResult> SearchUsers([FromQuery] string term, [FromQuery] bool? findLocked)
        {
            // Return an empty list if the search term is empty
            if (string.IsNullOrWhiteSpace(term)) 
            {
                return Json(new List<object>());
            }

            // Start with the base query: match the DisplayName to the search term
            var query = _userManager.Users
                .Where(u => u.DisplayName != null && u.DisplayName.Contains(term));

            // If the fetch call provided a lock status, apply the filter
            if (findLocked.HasValue)
            {
                query = query.Where(u => u.AccountLocked == findLocked.Value);
            }

            // Project the results into an anonymous object and limit to 10 results
            var matches = await query
                .Select(u => new 
                { 
                    displayName = u.DisplayName, 
                    email = u.Email 
                })
                .Take(10) 
                .ToListAsync();

            return Json(matches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAccountLock(string targetEmail, string adminPassword, bool shouldLock, string? auditDetails)
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

            // Capture the original lock state to prevent logging redundant audits
            bool originalLockState = targetUser.AccountLocked;

            var result = await _userService.LockToggleAccountAsync(targetUser, shouldLock);

            if (result)
            {
                // Only log an audit if the state actually changed, or if notes were provided
                if (originalLockState != shouldLock || !string.IsNullOrWhiteSpace(auditDetails))
                {
                    var audit = new AuditLog
                    {
                        ActionType = shouldLock ? AuditActionType.UserLocked : AuditActionType.UserUnlocked,
                        PerformingUserId = adminUser!.GuidId,
                        TargetUserId = targetUser.GuidId, // Use targetUser's ID
                        Details = string.IsNullOrWhiteSpace(auditDetails) ? null : auditDetails,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    await _auditService.LogActionAsync(audit);
                }

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