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

        // CSP-199: how many sightings the gallery shows per page. Within the ticket's
        // 10-20 range; this is THE policy decision — the service stays generic.
        private const int GalleryPageSize = 20;

        private readonly ILogger<SightingController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBadgeService _badgeService;
        private readonly IPhotoQualityService _photoQualityService;
        private readonly IAnimalFunFactService _funFactService;
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;

        public SightingController(ILogger<SightingController> logger, ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager, IBadgeService badgeService,
            IPhotoQualityService photoQualityService,
            IAnimalFunFactService funFactService,
            ICommentService commentService,
            IUserService userService)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
            _badgeService = badgeService;
            _photoQualityService = photoQualityService;
            _funFactService = funFactService;
            _commentService = commentService;
            _userService = userService;
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

            // CSP-187: load comments (filtered for hidden + blocked authors) and resolve author names.
            var currentUser = await _userManager.GetUserAsync(User);
            Guid? viewerId = currentUser?.GuidId;
            viewModel.ViewerCanModerate = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            // CSP-37: only the owner sees the edit button. Server re-checks on the edit routes.
            viewModel.CanEdit = currentUser != null && sighting.UserId == currentUser.GuidId;

            var comments = (await _commentService.GetCommentsForSightingAsync(id, viewerId)).ToList();
            var rows = new List<CommentRowViewModel>(comments.Count);
            foreach (var c in comments)
            {
                var authorId = Guid.Parse(c.AuthorIdentityId);
                var author = await _userService.GetUserByIdAsync(authorId);
                rows.Add(new CommentRowViewModel
                {
                    Id = c.Id,
                    AuthorId = authorId,
                    AuthorDisplayName = author?.DisplayName ?? "Unknown",
                    Body = c.Body,
                    CreatedAt = TimeZoneInfo.ConvertTime(c.CreatedAt, userZone),
                });
            }
            viewModel.Comments = rows;

            return View(viewModel);
        }

        #endregion

        #region CSP-37: Edit Sighting

        // GET /Sighting/Edit/{id}. Owner-only: pre-populates the editable fields plus the
        // read-only context (photo / GPS / timestamp). Non-owners are bounced to login
        // (the app's standard access-denied signal); unknown ids render the NotFound view.
        [HttpGet]
        [Route("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var sighting = await _sightingsService.GetSightingByIdAsync(id);
            if (sighting is null)
            {
                _logger.LogInformation("Edit page requested for unknown sighting {SightingId}", id);
                return View("NotFound");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                _logger.LogError("Authenticated user could not be found during Edit GET.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            if (sighting.UserId != user.GuidId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to open the edit page for sighting {SightingId} they do not own",
                    user.GuidId, id);
                // 403 → cookie auth redirects to /Account/AccessDenied. A plain login redirect
                // would not stick for an authenticated user — the login page bounces them onward.
                return Forbid();
            }

            return View(new SightingEditViewModel(sighting));
        }

        // POST /Sighting/Edit/{id}. Re-checks ownership server-side before validating or saving,
        // so a hidden/forged form can never edit someone else's sighting. On success, redirects
        // to Details with a flash message; on validation failure, redraws the form with the
        // user's input preserved and the read-only context re-hydrated.
        [HttpPost]
        [Route("Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SightingEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                _logger.LogError("Authenticated user could not be found during Edit POST.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var sighting = await _sightingsService.GetSightingByIdAsync(id);
            if (sighting is null)
            {
                _logger.LogInformation("Edit POST for unknown sighting {SightingId}", id);
                return View("NotFound");
            }

            if (sighting.UserId != user.GuidId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to submit an edit for sighting {SightingId} they do not own",
                    user.GuidId, id);
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                // Rebuild from the stored sighting to restore the read-only context (photo / GPS /
                // timestamp), then overlay the user's attempted edits so their input + the
                // validation messages both show.
                var redisplay = new SightingEditViewModel(sighting)
                {
                    Description = model.Description,
                    SpeciesName = model.SpeciesName
                };
                return View(redisplay);
            }

            await _sightingsService.UpdateSightingAsync(id, user.GuidId, model.Description, model.SpeciesName);
            TempData["EditSuccess"] = "Your sighting was updated.";
            return RedirectToAction("Details", new { id });
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

            // CSP-189: Reject Low-tier photos at the form level so the user gets a clear
            // "try again" experience instead of a confusing "saved but try again" flash.
            if (quality.Tier == PhotoQualityTier.Low)
            {
                ModelState.AddModelError(nameof(sightingUpload.UploadedImage),
                    _photoQualityService.GetLowQualityReasonMessage(quality.Sharpness, quality.Luminance));
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

            // CSP-189: Only one positive outcome now — Low is rejected above, so by here
            // the photo is High or Medium and we acknowledge it with a single confirmation.
            TempData["PhotoQualitySuccess"] = "Great photo! Upload accepted.";

            // Since invalid Sightings were already checked and the sighting has already been uploaded,
            // give the user the First Sighting Badge

            // Need to fetch the global timezone cookie for notification display purposes
            // Default timezone is PST
                     string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            await _badgeService.AddBadge(user, BadgeId.FirstSightingBadgeGUID, userTimeZoneId);
            await _badgeService.UpdateBadge(user, BadgeId.SightingNoviceBadgeGUID, userTimeZoneId);
            return RedirectToAction("Index", "Dashboard");
        }

        #region CSP-145 / CSP-96: Sighting Gallery Feature

        // CSP-96: Displays a community-wide gallery of all sightings.
        // Authenticated users can filter client-side to see only their own sightings
        // (filter operates on the current page only — see CSP-199 follow-up).
        // CSP-199: page is 1-based; default 1 keeps existing /Sighting/Gallery URLs working.
        [HttpGet]
        [Route("Gallery")]
        public async Task<IActionResult> Gallery(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database during Gallery access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            // CSP-199: Load only the requested page of sightings + their images instead of
            // the entire dataset. only ~PageSize rows (and their
            // image bytes) leave the database per request, not every sighting on record.
            var pageResult = await _sightingsService.GetSightingsPageAsync(page, GalleryPageSize);
            var viewModel = new SightingGalleryViewModel(pageResult, user.Id);

            _logger.LogInformation(
                "User {UserId} accessed community gallery page {Page} ({Count} of {Total} sightings)",
                user.Id, viewModel.CurrentPage, viewModel.SightingCount, viewModel.TotalCount);
            
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
