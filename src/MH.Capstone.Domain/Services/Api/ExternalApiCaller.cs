using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using MH.Capstone.Domain.ApiContracts;

namespace MH.Capstone.Domain.Services.Api
{
    public class ExternalApiCaller<TConfig> : IApiCaller<TConfig> where TConfig : class, IApiConfigurationValues
    {
        public string ClientName { get; }
        public TConfig ApiClientConfig { get; }

        private readonly ILogger<IApiCaller<TConfig>> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiCaller(ILogger<IApiCaller<TConfig>> logger, IHttpClientFactory httpClientFac,
            TConfig config)
        {
            _logger = logger;
            _httpClientFactory = httpClientFac;
            ClientName = config.HttpClientKey;
            ApiClientConfig = config;
        }

        /// <summary>
        /// Makes a GET call to an external API that returns a deserialized object of type T. The caller is responsible
        /// for ensuring that the API response can be deserialized to the requested type T. If not, a JsonException will be thrown.
        /// </summary>
        /// <typeparam name="TReturn">The type to deserialize the API return value to</typeparam>
        /// <param name="url">The url - excluding the base path - to send the request to.</param>
        /// <param name="queryParams">An optional <see cref="IEnumerable{T}"/> of </param>
        /// <returns>The deserialized <see cref="T"/> returned from the API</returns>
        /// <exception cref="HttpRequestException">The request failed.</exception>
        /// <exception cref="InvalidOperationException">Thrown by the <see cref="HttpClient"/> instance whe nan error occured before a call
        /// could be made. Most likely is caused by an invalid <see cref="ClientName"/> value</exception>
        /// <exception cref="JsonException">The <see cref="T"/> type could not be deserialized from the API's response.</exception>
        public async Task<TReturn> GetAsync<TReturn>(string url, params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            try
            {
                // The factory handles disposing of the HttpClient, so we don't need to worry about it here.
                var client = _httpClientFactory.CreateClient(ClientName);
                var queryList = queryParams?.ToList();
                if (queryList is { Count: > 0 })
                {
                    var queryString = string.Join("&", queryList.Select(kvp => 
                        $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    url = $"{url}?{queryString}";
                }

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TReturn>(content) ?? throw new JsonException("The api failed to deserialize " +
                    $"the api call response at {response.Headers.Location} to the requested type {typeof(TReturn)}");
            }
            // Exclude JsonExceptions from being caught here since we'll be throwing those ourselves with more context.
            catch (Exception ex) when (ex is not JsonException)
            {
                _logger.LogError(ex, "Error calling external API at {Url}", url);
                throw;
            }
        }
    }
}
