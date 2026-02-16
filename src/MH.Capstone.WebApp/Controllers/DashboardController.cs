using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{

    // restricts access to this controller so only authenticated users can access it
    [Authorize]
    public class DashboardController : Controller
    {
        // 2MB Image file limit.
        const long MAX_IMG_SIZE = 2 * 1024 * 1024;
        // Logger to track dashboard access and activity. 
        private readonly ILogger<DashboardController> _logger;

        // Dep. Injection for image upload and authentication services.
        private readonly IProfileImageService _imageService;

        private readonly IAuthenticationService _authService;

        // Constructor that injects the logger dependency
        public DashboardController(ILogger<DashboardController> logger, IProfileImageService imageService, IAuthenticationService authService)
        {
            _logger = logger;
            _imageService = imageService;
            _authService = authService;
        }

        // Displays the main dashboard page for authenticated users. 
        public IActionResult Index()
        {
            var userEmail = User.Identity?.Name;
            var user = _authService.GetUserByEmail(userEmail ?? "");
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);

            // Fetch the user profile image from the Model.
            // Defaults to the placeholder profile image if not found.
            if (user?.ProfileImage != null)
            {
                // Convert byte[] to Base64 string for HTML display
                string imageBase64 = Convert.ToBase64String(user.ProfileImage);
                ViewBag.ProfileImageUrl = $"data:image/jpeg;base64,{imageBase64}";
            }
            else
            {
                ViewBag.ProfileImageUrl = "/imgs/profileDefault.jpeg";
            }

            return View();
        }

        [HttpPost]
        // LINE BELOW HAS CS0161 ISSUE
        public async Task<IActionResult> UploadProfileImage(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var userEmail = User.Identity?.Name;

                // Reject the file if it's over 2MB
                if (profilePicture.Length > MAX_IMG_SIZE)
                {
                    _logger.LogWarning("Rejecting upload: File size {Size} exceeds 2MB limit.", profilePicture.Length);
                    return RedirectToAction("Index");
                }

                // Delegate to ProfileImageService
                byte[]? imageData = await _imageService.ConvertToBytesAsync(profilePicture);

                // Check for null before saving to DB
                if (userEmail != null && imageData != null && imageData.Length > 0)
                {
                    // Save the actual bytes to the database via the service
                    _authService.UpdateUserProfileImage(userEmail, imageData);
                    _logger.LogInformation("Profile image updated for user {Email}", userEmail);
                }
            }
            else 
            {
                _logger.LogWarning("Upload attempted with null or empty file.");
            }

            // Send this information back to the main dashboard page.
            return RedirectToAction("Index");
        }
    }
}