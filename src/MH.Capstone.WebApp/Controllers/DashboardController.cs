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

        // Constructor that injects the logger dependency
        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        // Displays the main dashboard page for authenticated users. 
        public IActionResult Index()
        {
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);
            return View();
        }
    }
}