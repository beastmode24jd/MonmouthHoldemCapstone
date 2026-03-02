using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public class ApiCallerFactory : IApiCallerFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IApiCallerFactory> _logger;
        private readonly ILogger<IApiCaller> _apiCallerLogger;

        public ApiCallerFactory(ILogger<IApiCallerFactory> logger, ILogger<IApiCaller> apiCallerLogger,
            IHttpClientFactory httpClientFac)
        {
            _logger = logger;
            _apiCallerLogger = apiCallerLogger;
            _httpClientFactory = httpClientFac;
        }

        /// <summary>
        /// Creates a new instance of an API caller configured for the specified client. This does not check if the client name is valid
        /// or if the necessary configuration exists; it simply creates an instance of <see cref="ExternalApiCaller"/> with the provided
        /// client name. The caller will be responsible for handling any errors related to invalid client names or missing configuration
        /// when making API requests.
        /// </summary>
        /// <param name="clientName">The name of the client for which the API caller will be configured. Cannot be null or empty.</param>
        /// <returns>An <see cref="IApiCaller"/> instance configured to make API requests on behalf of the specified client.</returns>
        public IApiCaller CreateApiCaller(string clientName) =>
            new ExternalApiCaller(_apiCallerLogger, _httpClientFactory, clientName);
    }
}
