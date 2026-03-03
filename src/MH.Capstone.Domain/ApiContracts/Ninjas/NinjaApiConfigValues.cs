using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.ApiContracts.Ninjas
{
    public class NinjaApiConfigValues : IApiConfigurationValues
    {
        public string HttpClientKey { get; }

        public string BaseUrl { get; }

        public List<KeyValuePair<string, string>> Endpoints { get; }
        
        public bool IsValid => !string.IsNullOrWhiteSpace(HttpClientKey) && 
                               !string.IsNullOrWhiteSpace(BaseUrl);

        public static T Create<T>(string httpClientKey,
            string baseUrl, List<KeyValuePair<string, string>> endpoints)
            where T : class, IApiConfigurationValues =>
                new NinjaApiConfigValues(httpClientKey, baseUrl, endpoints);

        public NinjaApiConfigValues(string httpClientKey,
            string baseUrl, IEnumerable<KeyValuePair<string, string>> endpoints)
        {
            HttpClientKey = httpClientKey;
            BaseUrl = baseUrl;
            Endpoints = endpoints.ToList();
        }
    }
}
