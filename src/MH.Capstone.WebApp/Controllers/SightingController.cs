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
        // CSP-122: long side below this many pixels is rejected outright at upload.
        private const int MinAcceptableLongSidePixels = 1024;

        private readonly ILogger<SightingController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;
        private readonly IPhotoQualityService _photoQualityService;
        private readonly IAnimalFunFactService _funFactService;

        public SightingController(ILogger<SightingController> logger, ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager, IBadgeService badgeService,
            IPhotoQualityService photoQualityService,
            IAnimalFunFactService funFactService)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
            _badgeService = badgeService;
            _photoQualityService = photoQualityService;
            _funFactService = funFactService;
        }

        #region CSP-177: Offline Queue

        [HttpGet]
        [Route("OfflineQueue")]
        public async Task<IActionResult> OfflineQueue()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found during OfflineQueue access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            ViewBag.CurrentUserId = user.Id;
            return View();
        }

        #endregion

        #region CSP-172: Sighting Details Page

        [HttpGet]
        [Route("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var sighting = await _sightingsService.GetSightingByIdAsync(id);
            if (sighting is null)
            {
                _logger.LogInformation("Sighting details requested for unknown id {SightingId}", id);
                return View("NotFound");
            }

            var funFact = await _funFactService.GetFunFactAsync(sighting.SpeciesName);
            var viewModel = new SightingDetailsViewModel(sighting, funFact);

            // Same timezone-cookie convention used by the Gallery action so the displayed
            // timestamp matches what the user saw on the card they clicked.
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";
            TimeZoneInfo userZone;
            try
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            viewModel.Timestamp = TimeZoneInfo.ConvertTime(viewModel.Timestamp, userZone);

            return View(viewModel);
        }

        #endregion

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
            if (!_sightingsService.ValidateImage(sightingUpload.UploadedImage))
                ModelState.AddModelError(nameof(sightingUpload.UploadedImage),
                    "Image must be a valid JPG or PNG file under 2 MB.");

            if (!ModelState.IsValid)
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

            // CSP-122: run the photo through the quality gate. Tier/scores are
            // informational, but resolution is a hard gate — anything below the
            // long-side threshold is rejected back to the upload form.
            var quality = await _photoQualityService.AnalyzeAsync(dataModel.ImageBuffer);

            int longSide = Math.Max(quality.Width, quality.Height);
            if (longSide < MinAcceptableLongSidePixels)
            {
                ModelState.AddModelError(nameof(sightingUpload.UploadedImage),
                    "Please upload a higher-resolution original (at least 1024 px on the long side).");
                return View(sightingUpload);
            }

            dataModel.QualityTier = quality.Tier;
            dataModel.SharpnessScore = quality.Sharpness;
            dataModel.LuminanceAverage = quality.Luminance;
            dataModel.ResolutionWidth = quality.Width;
            dataModel.ResolutionHeight = quality.Height;

            // Points are awarded in the service, so we don't need to worry about that here.
            // CreateSightingAsync returns the points awarded for the sighting, but we don't
            // need to capture that here since the user will be able to see it reflected in
            // their profile and badges immediately after upload.
            _ = await _sightingsService.CreateSightingAsync(dataModel, sightingUpload.DeviceTimezone);

            // CSP-122: surface a flash message based on the photo's quality tier so the
            // user gets immediate feedback after the redirect to Dashboard. Medium tier
            // is intentionally silent (passed, but nothing to celebrate or warn about).
            SetPhotoQualityFlashMessage(quality.Tier, quality.Sharpness, quality.Luminance);

            // Since invalid Sightings were already checked and the sighting has already been uploaded,
            // give the user the First Sighting Badge

            // Need to fetch the global timezone cookie for notification display purposes
            // Default timezone is PST
                     string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            await _badgeService.AddBadge(user, BadgeId.FirstSightingBadgeGUID, userTimeZoneId);
            await _badgeService.UpdateBadge(user, BadgeId.SightingNoviceBadgeGUID, userTimeZoneId);
            return RedirectToAction("Index", "Dashboard");
        }

        // CSP-122: maps a photo-quality analysis result to a TempData flash message.
        // Keys are read by the Dashboard view after the post-upload redirect.
        //   - High  → success badge
        //   - Low   → warning, with text picked by which sub-criterion triggered Low
        //   - Other → no message (Medium passes silently; Unknown shouldn't reach here)
        private void SetPhotoQualityFlashMessage(PhotoQualityTier tier, double sharpness, double luminance)
        {
            const double DarkLuminanceThreshold = 0.20;
            const double WashedOutLuminanceThreshold = 0.85;

            switch (tier)
            {
                case PhotoQualityTier.High:
                    TempData["PhotoQualitySuccess"] = "Ready for ID - High Quality";
                    break;

                case PhotoQualityTier.Low:
                    // Lighting issues take priority over blur — when both are bad, the
                    // user-visible problem is almost always the lighting, and "blurry"
                    // is the catch-all for everything else (low sharpness).
                    if (luminance < DarkLuminanceThreshold)
                        TempData["PhotoQualityWarning"] = "This photo is too dark - try finding better light";
                    else if (luminance > WashedOutLuminanceThreshold)
                        TempData["PhotoQualityWarning"] = "This photo is washed out - try adjusting exposure";
                    else
                        TempData["PhotoQualityWarning"] = "This photo looks a bit blurry - steady your camera or try again";
                    break;
            }
        }

        #region CSP-145 / CSP-96: Sighting Gallery Feature

        // CSP-96: Displays a community-wide gallery of all sightings.
        // Authenticated users can filter client-side to see only their own sightings.
        [HttpGet]
        [Route("Gallery")]
        public async Task<IActionResult> Gallery()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database during Gallery access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            // Load all sightings with User navigation property for attribution
            var sightings = await _sightingsService.GetAllSightingsAsync();
            var viewModel = new SightingGalleryViewModel(sightings, user.Id);

            _logger.LogInformation("User {UserId} accessed community gallery with {Count} total sightings",
                user.Id, viewModel.SightingCount);
            
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

            // Adjust the offset, and show each Sighting with the device's local display time
            foreach (var sighting in viewModel.Sightings)
            {
                sighting.Timestamp = TimeZoneInfo.ConvertTime(sighting.Timestamp, userZone);
                
                // Correcting visual bugs for previous saved Sightings
                if (sighting.PointValue < 10 && sighting.PointValue >= 0)
                {
                    // Assign the front-end display the default value.
                    sighting.PointValue = 10;
                }

                if (sighting.RarityMultiplier == 0)
                {
                    // Invalid Rarity Multiplier, default to Common Rarity
                    sighting.RarityMultiplier = 1.0;
                }

                if (sighting.Rarity != "Common" && sighting.Rarity != "Mythic" && sighting.Rarity != "Rare")
                {
                    // Invalid Rarity value. Default to Common.
                    sighting.Rarity = "Common";
                }
            }

            _logger.LogInformation("User {Email} accessed gallery with {Count} sightings", 
                user.Email, viewModel.SightingCount);

            return View(viewModel);
        }

        #endregion
    }
}
