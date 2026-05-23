using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public class AnimalFunFactService : IAnimalFunFactService
    {
        private const string AnimalEndpointKey = "animal";
        private const string UnknownSpeciesSentinel = "Unknown";

        // CSP-214: when the verbatim species name finds nothing, retry with simpler single words.
        // Ignore very short words (e.g. "of") and cap the number of retries to bound API calls.
        private const int MinWordLength = 3;
        private const int MaxWordCandidates = 3;

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

            // CSP-214: the recorded species name often doesn't match an API animal name verbatim
            // (e.g. "Mallard Duck" vs the API's "Mallard"), so the verbatim query returns nothing.
            // Try the full name first, then progressively simpler single-word candidates.
            foreach (var query in BuildQueryCandidates(speciesName))
            {
                var funFact = await TryGetFunFactForQueryAsync(endpointUrl, query);
                if (!string.IsNullOrWhiteSpace(funFact))
                {
                    return funFact;
                }
            }

            return null;
        }

        private async Task<string?> TryGetFunFactForQueryAsync(string endpointUrl, string query)
        {
            IEnumerable<AnimalApiDto>? response;
            try
            {
                response = await _ninjaApiCaller.GetAsync<IEnumerable<AnimalApiDto>>(
                    endpointUrl,
                    new KeyValuePair<string, string>("name", query));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSP-172: Animals API lookup for '{Species}' failed; falling back.", query);
                return null;
            }

            // CSP-214: the Animals API returns several fuzzy name matches and many of them
            // are sparse (no slogan/feature/lifestyle). Scanning only the first match made the
            // fun fact work for some species and silently fall back for others. Walk every
            // match and return the first one that yields a usable fact.
            foreach (var animal in response ?? Enumerable.Empty<AnimalApiDto>())
            {
                if (animal?.characteristics is null)
                {
                    continue;
                }

                var funFact = PickFunFact(animal.characteristics);
                if (!string.IsNullOrWhiteSpace(funFact))
                {
                    return funFact;
                }
            }

            return null;
        }

        // Yields the verbatim (trimmed) name first, then its individual words longest-first.
        // Longest words are the most distinctive (e.g. "Mallard" before "Duck"), and we skip
        // words equal to the full name so single-word species make exactly one API call.
        private static IEnumerable<string> BuildQueryCandidates(string speciesName)
        {
            var trimmed = speciesName.Trim();
            yield return trimmed;

            var words = trimmed
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= MinWordLength &&
                            !string.Equals(w, trimmed, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(w => w.Length)
                .Take(MaxWordCandidates);

            foreach (var word in words)
            {
                yield return word;
            }
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
