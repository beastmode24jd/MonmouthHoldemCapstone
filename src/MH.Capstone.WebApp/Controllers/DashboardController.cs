using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable InvertIf

namespace MH.Capstone.WebApp.Controllers
{

    // restricts access to this controller so only authenticated users can access it
    [Authorize]
    public class DashboardController : Controller
    {
        // 2MB Image file limit.
        // TODO - Consider moving this to a configuration file or constant class for better maintainability.
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
        public async Task<IActionResult> Index([FromQuery] bool sighting_success = false)
        {
            var userEmail = User.Identity?.Name;
            var user = await _authService.GetUserByEmailAsync(userEmail ?? "");
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);
            
            var statusMsgHtml = "<p>You are successfully logged in. Time to explore our nature, together!</p>";
            if (sighting_success)
            {
                statusMsgHtml = "<p class='fw-bold'>Congratulations! Your Sighting was uploaded successfully!</p>";
            }

            ViewData["statusMsgHtml"] = statusMsgHtml;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile? profilePicture)
        {
            // Clear and possible outstanding ModelState errors to ensure a clean slate for the view.
            // Doing this since we use custom modelErrors in the POST action, so ASP.NET's default
            // validation service won't clear our errors for us.
            ModelState.Clear();

            // Ensures not null and has content before processing. Logs a warning if the file is null or empty.
            if (profilePicture is { Length: > 0 })
            {
                var userEmail = User.Identity?.Name;

                // Reject the file if it's over 2MB
                if (profilePicture.Length > MAX_IMG_SIZE)
                {
                    _logger.LogWarning("Rejecting upload: File size {Size} exceeds 2MB limit.", profilePicture.Length);
                    ModelState.AddModelError(nameof(profilePicture), "File size exceeds the 2MB limit.");
                    return RedirectToAction("Index");
                }

                // Reject if not an image file based on content type
                // (basic check, can be bypassed but serves as a first line of defense)
                // TODO - Consider implementing a more robust file type validation
                // (e.g., checking file signatures) for better security.
                if (!profilePicture.ContentType.StartsWith("image/"))
                {
                    _logger.LogWarning("Rejecting upload: Invalid content type {ContentType}.", profilePicture.ContentType);
                    ModelState.AddModelError(nameof(profilePicture), "Invalid file type. Please upload an image.");
                    return RedirectToAction("Index");
                }

                // Delegate to ProfileImageService
                var imageData = await _imageService.ConvertToBytesAsync(profilePicture);

                // Check for null before saving to DB
                if (userEmail != null && imageData is { Length: > 0 })
                {
                    // Save the actual bytes to the database via the service
                    await _authService.UpdateUserProfileImageAsync(userEmail, imageData, profilePicture.ContentType);
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