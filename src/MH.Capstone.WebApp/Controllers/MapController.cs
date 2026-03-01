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
            // For now, return empty array since sightings on map is a future feature (CSP-99)
            // This endpoint will be used to fetch sightings within the map's visible bounds
            var sightings = new List<object>();
            
            _logger.LogInformation("Fetched {Count} sightings for map view", sightings.Count);

            // Using Task.FromResult to simulate async behavior since we are not fetching real data yet
            return await Task.FromResult(Json(sightings));
        }
    }
}