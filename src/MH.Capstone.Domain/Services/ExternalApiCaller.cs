using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MH.Capstone.Domain.Services
{
    public class ExternalApiCaller : IApiCaller
    {
        private readonly string _clientName;
        private readonly ILogger<IApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiCaller(ILogger<IApiCaller> logger, IHttpClientFactory httpClientFac, 
            string clientName)
        {
            _logger = logger;
            _httpClientFactory = httpClientFac;
            _clientName = clientName;
        }


        public async Task<T> GetAsync<T>(string url)
        {
            // The factory handles disposing of the HttpClient, so we don't need to worry about it here.
            var client = _httpClientFactory.CreateClient(_clientName);
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content) ?? throw new JsonException("The api failed to deserialize " +
                    $"the api call response at to the requested type {typeof(T)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API at {Url}", url);
                throw;
            }
        }
    }
}
