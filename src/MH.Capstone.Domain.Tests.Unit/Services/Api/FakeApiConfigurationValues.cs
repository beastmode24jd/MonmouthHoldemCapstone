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
    internal class ApiConfigurationValuesFake : ApiConfigurationValues<ApiConfigurationValuesFake>
    {
        public ApiConfigurationValuesFake() 
            : base("Test", "http://example.com/api/v1", [])
        { }

        public override ApiConfigurationValuesFake Create(string httpClientKey, string baseUrl,
            List<KeyValuePair<string, string>> endpoints) => new();
    }
}
