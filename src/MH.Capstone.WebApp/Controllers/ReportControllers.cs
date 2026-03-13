using System.Net;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // blocks unauthenticated users from submitting reports
    [Authorize]
    [Route("Report")]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ILogger<ReportController> logger,
            IReportService reportService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _reportService = reportService;
            _userManager = userManager;
        }

        // security to validate that request came from a form on our site, and not else where
        [HttpPost]
        [Route("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromForm] ReportSubmitViewModel model)
        {
<<<<<<< HEAD
            
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Please fill out all required fields." });
=======
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
>>>>>>> db1d61e ( implemented integration testing to complete GREEN, creating view model, controller, and testing environment)
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database.");
<<<<<<< HEAD
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Server error. Please try again." });
=======
                return StatusCode((int)HttpStatusCode.InternalServerError);
>>>>>>> db1d61e ( implemented integration testing to complete GREEN, creating view model, controller, and testing environment)
            }

            var report = new Report
            {
                ReportingUserId = user.GuidId,
                ReportedPageUrl = model.ReportedPageUrl,
                Reason = model.Reason,
                Description = model.Description,
                SubmittedAt = DateTime.UtcNow
            };

            var submitted = await _reportService.SubmitReportAsync(report);

            if (!submitted)
            {
                _logger.LogInformation("Duplicate report rejected for user {UserId} on {Url}",
                    user.Id, model.ReportedPageUrl);
<<<<<<< HEAD
                return Conflict(new { message = "You have already reported this page." });
            }

            return Ok(new { message = "Your report has been received. Thank you." });
=======
                return Conflict("You have already reported this page.");
            }

            TempData["ReportSuccess"] = "Your report has been received. Thank you.";
            return Redirect(model.ReportedPageUrl);
>>>>>>> db1d61e ( implemented integration testing to complete GREEN, creating view model, controller, and testing environment)
        }
    }
}