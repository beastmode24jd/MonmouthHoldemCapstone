using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public class AnimalFunFactService : IAnimalFunFactService
    {
        private readonly ILogger<AnimalFunFactService> _logger;
        private readonly IApiCaller<NinjaApiConfigValues> _ninjaApiCaller;

        public AnimalFunFactService(
            ILogger<AnimalFunFactService> logger,
            IApiCaller<NinjaApiConfigValues> ninjaApiCaller)
        {
            _logger = logger;
            _ninjaApiCaller = ninjaApiCaller;
        }

        public Task<string?> GetFunFactAsync(string speciesName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException("CSP-172: AnimalFunFactService is not implemented yet.");
    }
}
