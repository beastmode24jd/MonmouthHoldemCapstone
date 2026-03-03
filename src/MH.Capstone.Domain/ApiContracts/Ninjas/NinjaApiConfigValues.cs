using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.ApiContracts.Ninjas
{
    public class NinjaApiConfigValues : ApiConfigurationValues<NinjaApiConfigValues>
    {
        public override NinjaApiConfigValues Create(string httpClientKey,
            string baseUrl, List<KeyValuePair<string, string>> endpoints)
                =>  new NinjaApiConfigValues(httpClientKey, baseUrl, endpoints);

        public NinjaApiConfigValues(string httpClientKey,
            string baseUrl, IEnumerable<KeyValuePair<string, string>> endpoints)
                : base(httpClientKey, baseUrl, endpoints.ToList()) { }
    }
}
