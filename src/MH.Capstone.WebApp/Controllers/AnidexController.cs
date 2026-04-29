using System.Net;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    /// <summary>
    /// CSP-142: Personal Anidex collection — gallery of unique species the
    /// authenticated user has confirmed via their sightings.
    /// </summary>
    [Authorize]
    [Route("anidex")]
    public class AnidexController : Controller
    {
        private readonly ILogger<AnidexController> _logger;
        private readonly ISightingsService _sightingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnidexController(ILogger<AnidexController> logger,
            ISightingsService sightingsService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _sightingsService = sightingsService;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError("Authenticated user could not be found in the database during Anidex access.");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var entries = await _sightingsService.GetUserAnidexAsync(user.GuidId);
            var viewModel = new AnidexViewModel(entries);

            _logger.LogInformation("User {UserId} viewed Anidex with {Count} species.",
                user.Id, viewModel.TotalSpecies);

            return View(viewModel);
        }
    }
}
