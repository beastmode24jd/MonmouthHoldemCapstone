using System.Net;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.ApiContracts.Ninjas;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace MH.Capstone.WebApp.Controllers
{
    [Route("animal")]
    [Route("species")]
    public class SpeciesController : Controller
    {
        private readonly ILogger<SpeciesController> _logger;
        private readonly IApiCallerFactory<NinjaApiConfigValues> _ninjaApiCallerFactory;

        public SpeciesController(ILogger<SpeciesController> logger,
            IApiCallerFactory<NinjaApiConfigValues> ninjaApiCallerFactory)
        {
            _logger = logger;
            _ninjaApiCallerFactory = ninjaApiCallerFactory;
        }

        [HttpGet]
        [Route("search")]
        public IActionResult Search()
        {
            return View();
        }

        [HttpGet]
        [Route("search/by-name")]
        [ValidateAntiForgeryToken]
        [Produces(typeof(IEnumerable<AnimalApiDto>))]
        public async Task<IActionResult> SearchByName([FromQuery] string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogInformation($"Call made to our search action, but the name was null or empty.");
                return BadRequest();
            }

            _logger.LogDebug($"Call made to our search action for an animal/species with the name '{name}'.");

            var apiCaller = _ninjaApiCallerFactory.CreateApiCaller();

            try
            {
                _logger.LogInformation($"Config Endpoints Length: {apiCaller.ConfigValues.Endpoints.Count}");
                _logger.LogInformation($"Config ClientKey: {apiCaller.ConfigValues.HttpClientKey}");
                _logger.LogInformation($"Config BaseUrl: {apiCaller.ConfigValues.BaseUrl}");
                var url = apiCaller.ConfigValues.Endpoints
                    .FirstOrDefault(kvp => string.Equals(kvp.Key, "animal", StringComparison.InvariantCultureIgnoreCase))
                    .Value ?? throw new InvalidConfigurationException("The needed Animal endpoint could " +
                                                                      "not be found in the api caller's config values!");

                _logger.LogWarning($"This would be a call to the api! name = {name}");
                var result = Array.Empty<AnimalApiDto>().ToList();
                //var result = (await apiCaller.GetAsync<IEnumerable<AnimalApiDto>>(url, 
                //    new KeyValuePair<string, string>("name", name))).ToList();

                if (result.Count > 0)
                {
                    return Ok(result);
                }

                // No results found, log this case and return a 404 response
                _logger.LogInformation($"No animal/species found with the name '{name}'.");
                return NotFound();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Log the not found case and return a 404 response
                _logger.LogInformation(ex,
                    $"No animal/species found with the name '{name}'.");
                return NotFound();
            }
            catch (Exception ex)
            {
                // Log and gracefully handle any exceptions that may occur during the API call
                _logger.LogError(ex,
                    $"An error occurred while trying to search for an animal/species with the name '{name}'.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
