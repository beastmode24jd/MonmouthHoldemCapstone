using MH.Capstone.Domain.Services;
using MH.Capstone.WebApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    [Route("sighting")]
    public class SightingController : Controller
    {
        private readonly ILogger<SightingController> _logger;
        private readonly ISightingsService _sightingsService;
        //private readonly UserManager<ApplicationUser>

        public SightingController(ILogger<SightingController> logger, ISightingsService sightingsService)
        {
            _logger = logger;
            _sightingsService = sightingsService;
        }

        [HttpGet]
        [Route("Upload")]
        [Route("Create")]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [Route("Upload")]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(SightingUploadViewModel sightingUpload)
        {
            if (!ModelState.IsValid)
            {
                return View(sightingUpload);
            }

            // TODO: Get the user ID from the logged in user using the UserManager
            var dataModel = sightingUpload.ToDataModel(Guid.Empty);
            await _sightingsService.CreateSightingAsync(dataModel);
            return RedirectToAction("Index", "Dashboard", new { sighting_success = true });
        }
    }
}
