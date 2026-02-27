using System.Net;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
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

        public SightingController(ILogger<SightingController> logger, ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("Upload")]
        [Route("Create")]
        public IActionResult Upload()
        {
            return View(new SightingUploadViewModel());
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

            var dataModel = sightingUpload.ToDataModel(Guid.Parse(user.Id));
            int pointsEarned = await _sightingsService.CreateSightingAsync(dataModel);
            return RedirectToAction("Index", "Dashboard", new { sighting_success = true, points_earned = pointsEarned });
        }
    }
}
