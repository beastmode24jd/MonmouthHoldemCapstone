using MH.Capstone.Domain.Services.Abstraction;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.ApiContracts
{
    public abstract class ApiConfigurationValues<TConfig> where TConfig : ApiConfigurationValues<TConfig>
    {
        public string HttpClientKey { get; }
        
        public string BaseUrl { get; }

        public List<KeyValuePair<string, string>> Endpoints { get; }

        /// <summary>
        /// Indicates whether this configuration is in a valid state to be used for making API calls.
        /// This is determined by whether the required properties - <see cref="HttpClientKey"/> and
        /// <see cref="BaseUrl"/> - are not null or empty. Endpoints are not required since some APIs
        /// may have dynamic endpoints that are determined at runtime, so they are not included in this validation check.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating if this configuration property is currently in a valid state</returns>
        public virtual bool IsValid => !string.IsNullOrWhiteSpace(HttpClientKey) &&
                                       !string.IsNullOrWhiteSpace(BaseUrl);


        protected ApiConfigurationValues(string httpClientKey, string baseUrl,
            IEnumerable<KeyValuePair<string,string>> endpoints)
        {
            HttpClientKey = httpClientKey;
            BaseUrl = baseUrl;
            Endpoints = endpoints.ToList();
        }

        public abstract TConfig Create(string httpClientKey, string baseUrl,
            List<KeyValuePair<string, string>> endpoints);
    }
}
