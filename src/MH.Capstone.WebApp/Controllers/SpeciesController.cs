using MH.Capstone.Domain.ApiContracts.Ninjas;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    [Route("species")]
    public class SpeciesController : Controller
    {
        private readonly ILogger<SpeciesController> _logger;
        private readonly IApiCallerFactory<NinjaApiConfigValues> _apiCallerFactory;

        public SpeciesController(ILogger<SpeciesController> logger,
            IApiCallerFactory<NinjaApiConfigValues> apiCallerFactory)
        {
            _logger = logger;
            _apiCallerFactory = apiCallerFactory;
        }

        [HttpGet]
        [Route("search")]
        public IActionResult Search()
        {
            return View();
        }

        //[HttpGet]
        //[Route("search/by-name")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Search([FromQuery] string? name)
        //{
        //    if (string.IsNullOrEmpty(name))
        //    {
        //        _logger.LogInformation($"Call made to our search action, but the name was null or empty.");
        //        return View();
        //    }

        //    _logger.LogDebug($"Call made to our search action for an animal/species with the name '{name}'.");

        //    var apiCaller = _apiCallerFactory.CreateApiCaller();
        //}
    }
}
