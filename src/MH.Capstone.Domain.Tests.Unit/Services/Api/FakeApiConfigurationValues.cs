using MH.Capstone.Domain.ApiContracts;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api
{
    internal class ApiConfigurationValuesFake : IApiConfigurationValues
    {
        public ApiConfigurationValuesFake()
        {
            
        }

        public static ApiConfigurationValuesFake Instance = new();

        public string HttpClientKey { get; }

        public string BaseUrl { get; }

        public List<KeyValuePair<string, string>> Endpoints { get; }

        public bool IsValid { get; }

        public static T Create<T>(string httpClientKey, string baseUrl, 
            List<KeyValuePair<string, string>> endpoints) where T 
            : class, IApiConfigurationValues => new ApiConfigurationValuesFake() as T;

    }
}
