using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.Tools;

namespace MH.Capstone.Domain.Services.Api
{
    public sealed class ExternalApiCaller<TConfig> : IApiCaller<TConfig> where TConfig : ApiConfigurationValues<TConfig>
    {
        public string ClientName { get; }
        
        public TConfig ConfigValues { get; }

        private readonly ILogger<IApiCaller<TConfig>> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiCaller(ILogger<IApiCaller<TConfig>> logger, IHttpClientFactory httpClientFac,
            TConfig configValues)
        {
            _logger = logger;
            _httpClientFactory = httpClientFac;
            ClientName = configValues.HttpClientKey;
            ConfigValues = configValues;
        }

        /// <summary>
        /// Makes a GET call to an external API that returns a deserialized object of type T. The caller is responsible
        /// for ensuring that the API response can be deserialized to the requested type T. If not, a JsonException will be thrown.
        /// </summary>
        /// <typeparam name="TReturn">The type to deserialize the API return value to</typeparam>
        /// <param name="url">The url - excluding the base path - to send the request to.</param>
        /// <param name="queryParams">An optional <see cref="IEnumerable{T}"/> of </param>
        /// <returns>The deserialized <see cref="TReturn"/> returned from the API</returns>
        /// <exception cref="HttpRequestException">The request failed.</exception>
        /// <exception cref="InvalidOperationException">Thrown by the <see cref="HttpClient"/> instance whe nan error occured before a call
        /// could be made. Most likely is caused by an invalid <see cref="ClientName"/> value</exception>
        /// <exception cref="JsonException">The <see cref="TReturn"/> type could not be deserialized from the API's response.</exception>
        public async Task<TReturn> GetAsync<TReturn>(string url, params IEnumerable<KeyValuePair<string, string>>? queryParams) 
            where TReturn : class
        {
            try
            {
                // The factory handles disposing of the HttpClient, so we don't need to worry about it here.
                var client = _httpClientFactory.CreateClient(ClientName);
                url = $"{url}?{HttpHelperMethods.CreateQueryParamsFragment(queryParams)}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogCritical($"Content String:\n\n{content}");
                return JsonConvert.DeserializeObject<TReturn>(content, ConfigValues.JsonSerializerSettings) 
                       ?? throw new JsonException("The api failed to deserialize " +
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
