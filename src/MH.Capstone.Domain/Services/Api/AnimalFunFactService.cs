using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public class AnimalFunFactService : IAnimalFunFactService
    {
        private const string AnimalEndpointKey = "animal";
        private const string UnknownSpeciesSentinel = "Unknown";

        private readonly ILogger<AnimalFunFactService> _logger;
        private readonly IApiCaller<NinjaApiConfigValues> _ninjaApiCaller;

        public AnimalFunFactService(
            ILogger<AnimalFunFactService> logger,
            IApiCaller<NinjaApiConfigValues> ninjaApiCaller)
        {
            _logger = logger;
            _ninjaApiCaller = ninjaApiCaller;
        }

        public async Task<string?> GetFunFactAsync(string speciesName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(speciesName) ||
                string.Equals(speciesName.Trim(), UnknownSpeciesSentinel, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var endpointUrl = _ninjaApiCaller.ConfigValues.Endpoints
                .FirstOrDefault(kvp => string.Equals(kvp.Key, AnimalEndpointKey, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (string.IsNullOrEmpty(endpointUrl))
            {
                _logger.LogWarning("CSP-172: Animals API endpoint '{Key}' is not configured; skipping fun-fact lookup.",
                    AnimalEndpointKey);
                return null;
            }

            IEnumerable<AnimalApiDto>? response;
            try
            {
                response = await _ninjaApiCaller.GetAsync<IEnumerable<AnimalApiDto>>(
                    endpointUrl,
                    new KeyValuePair<string, string>("name", speciesName));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSP-172: Animals API lookup for '{Species}' failed; falling back.", speciesName);
                return null;
            }

            var first = response?.FirstOrDefault();
            if (first?.characteristics is null)
            {
                return null;
            }

            return PickFunFact(first.characteristics);
        }

        private static string? PickFunFact(AnimalApiCharacteristics c)
        {
            if (!string.IsNullOrWhiteSpace(c.slogan)) return c.slogan;
            if (!string.IsNullOrWhiteSpace(c.mostDistinctiveFeature)) return c.mostDistinctiveFeature;
            if (!string.IsNullOrWhiteSpace(c.lifestyle)) return c.lifestyle;
            return null;
        }
    }
}
