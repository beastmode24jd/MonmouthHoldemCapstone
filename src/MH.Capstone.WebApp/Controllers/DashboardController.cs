using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{

    // restricts access to this controller so only authenticated users can access it
    [Authorize]
    public class DashboardController : Controller
    {
        // Logger to track dashboard access and activity. 
        private readonly ILogger<DashboardController> _logger;

        // Dep. Injection for image upload feature.
         private readonly IProfileImageService _imageService;

        // Constructor that injects the logger dependency
        public DashboardController(ILogger<DashboardController> logger, IProfileImageService imageService)
        {
            _logger = logger;
            _imageService = _imageService;
        }

        // Displays the main dashboard page for authenticated users. 
        public IActionResult Index()
        {
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);
            // Default value, would fetch image URL from DB if properly connected.
            ViewBag.ProfileImageUrl = null;
            return View();
        }

        public async Task<IActionResult> UploadProfileImage(IFormFile profilePic)
        {
            if (profilePic != null)
            {
                var imageUrl = await _imageService.UploadImageAsync(profilePic);
                // Stubbed, would save 'imageUrl' to local DB otherwise?
                _logger.LogInformation($"Image uploaded to {imageUrl}");
            }
            // Send this information back to the main dashboard page.
            return RedirectToAction("Index");
        }
    }
}