using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MH.Capstone.Domain.Services.Api
{
    public class ExternalApiCaller : IApiCaller
    {
        public string ClientName { get; }
        private readonly ILogger<IApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiCaller(ILogger<IApiCaller> logger, IHttpClientFactory httpClientFac, 
            string clientName)
        {
            _logger = logger;
            _httpClientFactory = httpClientFac;
            ClientName = clientName;
        }

        /// <summary>
        /// Makes a GET call to an external API that returns a deserialized object of type T. The caller is responsible
        /// for ensuring that the API response can be deserialized to the requested type T. If not, a JsonException will be thrown.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the API return value to</typeparam>
        /// <param name="url">The url - excluding the base path - to send the request to.</param>
        /// <returns>The deserialized <see cref="T"/> returned from the API</returns>
        /// <exception cref="HttpRequestException">The request failed.</exception>
        /// <exception cref="InvalidOperationException">Thrown by the <see cref="HttpClient"/> instance whe nan error occured before a call
        /// could be made. Most likely is caused by an invalid <see cref="ClientName"/> value</exception>
        /// <exception cref="JsonException">The <see cref="T"/> type could not be deserialized from the API's response.</exception>
        public async Task<T> GetAsync<T>(string url)
        {
            // The factory handles disposing of the HttpClient, so we don't need to worry about it here.
            var client = _httpClientFactory.CreateClient(ClientName);
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content) ?? throw new JsonException("The api failed to deserialize " +
                    $"the api call response at {response.Headers.Location} to the requested type {typeof(T)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API at {Url}", url);
                throw;
            }
        }
    }
}
