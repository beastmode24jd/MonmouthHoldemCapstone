using System.Net;
using MH.Capstone.Domain.Constants;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    [Route("Sighting")]
    public class SightingController : Controller
    {
        private readonly ILogger<SightingController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;

        public SightingController(ILogger<SightingController> logger, ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager, IBadgeService badgeService)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
            _badgeService = badgeService;
        }

        [HttpGet]
        [Route("Upload")]
        [Route("Create")]
        public IActionResult Upload()
        {
            // Get timezone cookie from site.js (defaults to PST if not found)
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";
            
            TimeZoneInfo userZone;

            try
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                // Windows server fallback for if IANA string fails
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }

            // Convert UTC to user's local timezone
            DateTimeOffset localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, userZone);

            // Initialize SightingUploadViewModel w/ local time and timezone ID
            var viewModel = new SightingUploadViewModel
            {
                Timestamp = localNow,
                DeviceTimezone = userTimeZoneId
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route("Upload")]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([FromForm] SightingUploadViewModel sightingUpload)
        {
            if (!ModelState.IsValid || !_sightingsService.ValidateImage(sightingUpload.UploadedImage))
            {
                _logger.LogInformation("Invalid sighting model was submitted and rejected.\n" +
                                       $"ModelState: {ModelState.IsValid}\n" +
                                       $"ModelState Err Count: {ModelState.ErrorCount}\n" +
                                       $"Image Null? {sightingUpload.UploadedImage is null}\n" +
                                       $"ValidateImage Result: {_sightingsService.ValidateImage(sightingUpload.UploadedImage)}");
                return View(sightingUpload);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var dataModel = sightingUpload.ToDataModel(user.GuidId);
            // Points are awarded in the service, so we don't need to worry about that here.
            // CreateSightingAsync returns the points awarded for the sighting, but we don't
            // need to capture that here since the user will be able to see it reflected in
            // their profile and badges immediately after upload.
            _ = await _sightingsService.CreateSightingAsync(dataModel, sightingUpload.DeviceTimezone);

            // Since invalid Sightings were already checked and the sighting has already been uploaded,
            // give the user the First Sighting Badge

            // Need to fetch the global timezone cookie for notification display purposes
            // Default timezone is PST
                     string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            await _badgeService.AddBadge(user, BadgeId.FirstSightingBadgeGUID, userTimeZoneId);
            return RedirectToAction("Index", "Dashboard");
        }

        #region CSP-145: Sighting Gallery Feature

        
        // Displays a gallery view of all sightings uploaded by the authenticated user.
        // Shows an empty state if the user has no sightings.
       
        [HttpGet]
        [Route("Gallery")]
        public async Task<IActionResult> Gallery()
        {
            // Get the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // If user is not authenticated or not found, return Unauthorized
            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database during Gallery access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            // Fetch all sightings for this user from the service layer
            // The service handles filtering by userId and ordering by timestamp
            var sightings = await _sightingsService.GetUserSightingsAsync(user.GuidId);

            
            // Get the timezone from global cookie, defaults to PST
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

            // Convert the sightings to a ViewModel for display
            // This handles byte[] to base64 conversion for images
            var viewModel = new SightingGalleryViewModel(sightings);

            // Adjust the offset, and show each Sighting with the device's local display time
            foreach (var sighting in viewModel.Sightings)
            {
                sighting.Timestamp = TimeZoneInfo.ConvertTime(sighting.Timestamp, userZone);
            }

            _logger.LogInformation("User {Email} accessed gallery with {Count} sightings", 
                user.Email, viewModel.SightingCount);

            return View(viewModel);
        }

        #endregion
    }
}
