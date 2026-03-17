using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MH.Capstone.Domain.ApiContracts.Ninja
{
    public class NinjaApiConfigValues : ApiConfigurationValues<NinjaApiConfigValues>
    {
        public static JsonSerializerSettings GetJsonSerializerSettings =>
            new NinjaApiConfigValues().JsonSerializerSettings;

        public override NinjaApiConfigValues Create(string httpClientKey,
            string baseUrl, List<KeyValuePair<string, string>> endpoints)
                =>  new NinjaApiConfigValues(httpClientKey, baseUrl, endpoints);

        public NinjaApiConfigValues(string httpClientKey,
            string baseUrl, IEnumerable<KeyValuePair<string, string>> endpoints)
                : base(httpClientKey, baseUrl, endpoints.ToList()) { }

        private NinjaApiConfigValues() {}
    }
}
