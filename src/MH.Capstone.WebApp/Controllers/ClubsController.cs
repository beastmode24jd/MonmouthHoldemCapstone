using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    public class ClubsController : Controller
    {
        private readonly IClubService _clubService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClubsController(IClubService clubService, UserManager<ApplicationUser> userManager)
        {
            _clubService = clubService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var publicClubs = await _clubService.GetPublicClubsAsync();
            var userClubs = await _clubService.GetUserClubsAsync(user.GuidId);

            var viewModel = new ClubListViewModel(publicClubs, userClubs, user.Id);

            return View("LandingPage", viewModel);
        }
    }
}
