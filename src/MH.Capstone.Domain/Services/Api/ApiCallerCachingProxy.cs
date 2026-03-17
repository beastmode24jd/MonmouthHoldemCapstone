using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MH.Capstone.Domain.Services.Api
{
    public sealed class ApiCallerCachingProxy<TCacheEntity, TConfig> : IApiCaller<TConfig>
        where TCacheEntity : ApiCallerCacheEntity, new()
        where TConfig : ApiConfigurationValues<TConfig>
    {
        private readonly ILogger<ApiCallerCachingProxy<TCacheEntity, TConfig>> _logger;
        private readonly IRepository<TCacheEntity, ApplicationDbContext> _cacheRepo;
        private readonly IApiCaller<TConfig> _realApiCaller;

        public TConfig ConfigValues { get; }

        public ApiCallerCachingProxy(ILogger<ApiCallerCachingProxy<TCacheEntity, TConfig>> logger,
            IRepository<TCacheEntity, ApplicationDbContext> cacheRepo, IApiCaller<TConfig> realCaller,
            TConfig configValues)
        {
            _logger = logger;
            _cacheRepo = cacheRepo;
            _realApiCaller = realCaller;
            ConfigValues = configValues;
        }

        public async Task<TApiReturn> GetAsync<TApiReturn>(string url, params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            var queryList = queryParams?.ToList();
            var queryParamsStr = HttpHelperMethods.CreateQueryParamsFragment(queryList);
            var cachedResults = await TryGetCachedResults(url, queryParamsStr);
            if (cachedResults != null)
            {
                try
                {
                    _logger.LogInformation("Cache hit for URL: {Url} with query params: {QueryParams}", url, queryParams);
                    var result = JsonConvert.DeserializeObject<TApiReturn>(cachedResults.ApiJson, ConfigValues.JsonSerializerSettings)
                                 ?? throw new JsonException("A conversion error has occurred with a cached api result causing a failure to convert the " +
                                                            "cached JSON value. The record will be deleted", new Exception($"Json Value: {cachedResults}"));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while mapping/converting JSON from a cached entity.");
                    await _cacheRepo.DeleteAsync(cachedResults);
                }
            }

            _logger.LogInformation("Cache miss for URL: {Url} with query params: {QueryParams}. Calling real API.", url, queryParams);
            var apiResult = await _realApiCaller.GetAsync<TApiReturn>(url, queryList);
            await CacheResults(url, apiResult, queryParamsStr);
            return apiResult;
        }

        private async Task<TCacheEntity?> TryGetCachedResults(string url, string? queryParamsStr) =>
            (await _cacheRepo.GetAllAsync()).FirstOrDefault();

        private async Task CacheResults<TApiReturn>(string url, TApiReturn apiResult, string? queryParamsStr)
        {
            try
            {
                var apiJson = JsonConvert.SerializeObject(apiResult, ConfigValues.JsonSerializerSettings);
                var cacheEntity = new TCacheEntity
                {
                    Url = url,
                    QueryParams = queryParamsStr ?? string.Empty,
                    ApiJson = apiJson,
                    CachedAt = DateTime.UtcNow
                };
                await _cacheRepo.AddOrUpdateAsync(cacheEntity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "An error occurred while caching API results for URL: {Url} with query params: {QueryParams}", url,
                    queryParamsStr);
                throw;
            }
        }
    }
}
