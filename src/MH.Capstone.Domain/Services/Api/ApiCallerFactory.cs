using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public class ApiCallerFactory<TConfig> : IApiCallerFactory<TConfig> where TConfig : class, IApiConfigurationValues
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IApiCallerFactory<TConfig>> _logger;
        private readonly ILogger<IApiCaller<TConfig>> _apiCallerLogger;
        private readonly TConfig _config;

        public ApiCallerFactory(ILogger<IApiCallerFactory<TConfig>> logger, ILogger<IApiCaller<TConfig>> apiCallerLogger,
            IHttpClientFactory httpClientFac, TConfig config)
        {
            _logger = logger;
            _apiCallerLogger = apiCallerLogger;
            _httpClientFactory = httpClientFac;
            _config = config;
        }

        /// <summary>
        /// Creates a new instance of an API caller configured for the specified client. This does not check if the client name is valid
        /// or if the necessary configuration exists; it simply creates an instance of <see cref="ExternalApiCaller{T}"/> with the provided
        /// client name. The caller will be responsible for handling any errors related to invalid client names or missing configuration
        /// when making API requests.
        /// </summary>
        /// <returns>An <see cref="IApiCaller{T}"/> instance configured to make API requests on behalf of the specified client.</returns>
        public IApiCaller<TConfig> CreateApiCaller() => 
                new ExternalApiCaller<TConfig>(_apiCallerLogger, _httpClientFactory, _config);
    }
}
