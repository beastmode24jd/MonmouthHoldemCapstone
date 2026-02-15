using MH.Capstone.Domain.Services;
using MH.Capstone.WebApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
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

        [HttpPut]
        [Route("Upload")]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadSightingViewModel upload)
        {
            if (!ModelState.IsValid)
            {
                return View(upload);
            }

            // TODO: Get the user ID from the logged in user using the UserManager
            var dataModel = upload.ToDataModel(Guid.Empty);
            await _sightingsService.CreateSightingAsync(dataModel);
            return RedirectToAction("Index", "Dashboard", new { sighting_success = true });
        }
    }
}
