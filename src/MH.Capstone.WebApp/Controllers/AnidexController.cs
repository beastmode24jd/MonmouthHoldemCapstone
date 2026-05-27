using System.Net;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    /// <summary>
    /// CSP-142: Personal Anidex collection — gallery of unique species the
    /// authenticated user has confirmed via their sightings.
    /// </summary>
    [Authorize]
    [Route("anidex")]
    public class AnidexController : Controller
    {
        private readonly ILogger<AnidexController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnidexController(ILogger<AnidexController> logger,
            ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database during Anidex access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var entries = await _sightingsService.GetUserAnidexAsync(user.GuidId);
            var viewModel = new AnidexViewModel(entries);

            _logger.LogInformation("User {UserId} viewed Anidex with {Count} species.",
                user.Id, viewModel.TotalSpecies);

            return View(viewModel);
        }

        /// <summary>
        /// CSP-277: Serves a single sighting's photo so the Anidex page can lazy-load
        /// thumbnails instead of base64-inlining every image into the HTML document
        /// (which had bloated the page to ~23MB). Scoped to the requesting user — the
        /// Anidex is personal, so we never serve another user's photo.
        /// </summary>
        [HttpGet]
        [Route("image/{sightingId:guid}")]
        public async Task<IActionResult> Image(Guid sightingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var sighting = await _sightingsService.GetSightingByIdAsync(sightingId);

            // 404 (not 403) when the sighting is missing OR isn't the caller's, so the
            // endpoint never reveals whether another user's sighting id exists.
            if (sighting?.ImageBuffer is not { Length: > 0 } || sighting.UserId != user.GuidId)
            {
                return NotFound();
            }

            // Sighting photos are immutable once uploaded, so let the browser cache them.
            // This is what keeps lazy thumbnails cheap on scroll-back and revisits.
            Response.Headers.CacheControl = "private, max-age=86400";
            return File(sighting.ImageBuffer, "image/jpeg");
        }
    }
}
