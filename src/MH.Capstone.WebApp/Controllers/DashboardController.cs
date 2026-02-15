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
            _imageService = imageService;
        }

        // Displays the main dashboard page for authenticated users. 
        public IActionResult Index()
        {
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);
            // Ternary uses default profile image, if one isn't uploaded.
            // Passes through the ViewBag as TempData, upload should save to localDB.
            ViewBag.ProfileImageUrl = TempData["ProfileImageUrl"] ?? "/imgs/profileDefault.jpg";
            return View();
        }

        public async Task<IActionResult> UploadProfileImage(IFormFile profilePicture)
        {
            if (profilePicture != null)
            {
                var imageUrl = await _imageService.UploadImageAsync(profilePicture);
                // Stubbed, would save 'imageUrl' to local DB otherwise?
                _logger.LogInformation($"Image uploaded to {imageUrl}");
            }
            // Send this information back to the main dashboard page.
            return RedirectToAction("Index");
        }
    }
}