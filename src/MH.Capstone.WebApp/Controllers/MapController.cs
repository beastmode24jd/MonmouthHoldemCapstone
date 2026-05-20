using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    [Route("Map")]
    public class MapController : Controller
    {
        private readonly ILogger<MapController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MapController(
            ILogger<MapController> logger,
            ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            _logger.LogInformation("User {User} accessed the sightings map", User.Identity?.Name);
            return View();
        }

        [HttpGet]
        [Route("Sightings")]
        public async Task<IActionResult> GetSightings(double? minLat, double? maxLat, double? minLng, double? maxLng)
        {
            // If bounds not provided, return empty
            if (!minLat.HasValue || !maxLat.HasValue || !minLng.HasValue || !maxLng.HasValue)
            {
                return Json(new List<object>());
            }

            var sightings = await _sightingsService.GetSightingsInBoundsAsync(
                (decimal)minLat.Value,
                (decimal)maxLat.Value,
                (decimal)minLng.Value,
                (decimal)maxLng.Value);

            // [CSP-217] Same UserTimeZone cookie convention used by SightingController/DashboardController
            // so popup timestamps render in the user's local time, falling back to PST.
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

            // Map to anonymous objects for JSON response (don't send entire entity with image bytes)
            var result = sightings.Select(s => new
            {
                id = s.Id,
                lat = s.Latitude,
                lng = s.Longitude,
                description = s.Description,
                timestamp = TimeZoneInfo.ConvertTime(s.Timestamp, userZone).ToString("MMM dd, yyyy h:mm tt"),
                imageUrl = $"/Map/SightingImage/{s.Id}"
            });

            _logger.LogInformation("Fetched {Count} sightings for map view", result.Count());
            return Json(result);
        }

        [HttpGet]
        [Route("SightingImage/{id}")]
        public async Task<IActionResult> GetSightingImage(Guid id)
        {
            var sightings = await _sightingsService.GetSightingsInBoundsAsync(-90, 90, -180, 180);
            var sighting = sightings.FirstOrDefault(s => s.Id == id);

            if (sighting == null || sighting.ImageBuffer == null)
            {
                return NotFound();
            }

            return File(sighting.ImageBuffer, "image/jpeg");
        }
    }
}