using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Manage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Please provide a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Generic error as requested if user is not found
                TempData["Error"] = "Please provide a valid email address.";
                return RedirectToAction(nameof(Manage));
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["Success"] = $"User {email} has been promoted to Admin.";
            }
            else
            {
                TempData["Error"] = "Could not promote user to Admin role.";
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}