using MH.Capstone.Domain.ApiContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IApiCaller<out TConfig> where TConfig : ApiConfigurationValues<TConfig>
    {
        TConfig ConfigValues { get; }

        Task<T> GetAsync<T>(string url, params 
            IEnumerable<KeyValuePair<string, string>>? queryParams);
    }
}
