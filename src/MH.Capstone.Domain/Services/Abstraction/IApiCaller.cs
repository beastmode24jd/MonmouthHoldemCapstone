using MH.Capstone.Domain.ApiContracts;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IApiCaller<out TConfig> where TConfig : ApiConfigurationValues<TConfig>
    {
        TConfig ConfigValues { get; }

        Task<TDto> GetAsync<TDto>(string url, params IEnumerable<KeyValuePair<string, string>>? queryParams)
            where TDto : class;
    }
}