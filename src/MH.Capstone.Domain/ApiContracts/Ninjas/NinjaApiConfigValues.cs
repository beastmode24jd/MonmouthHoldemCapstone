using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.ApiContracts.Ninjas
{
    public class NinjaApiConfigValues : ApiConfigurationValues
    {
        public NinjaApiConfigValues(string httpClientKey, 
            string baseUrl, 
            KeyValuePair<string, string>[] endpoints) 
            : base(httpClientKey, baseUrl, endpoints)
        { }
    }
}
