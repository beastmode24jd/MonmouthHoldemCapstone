using System.Linq;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Api
{
    public sealed class ApiCallerCachingProxy<TApiDto, TCacheEntity, TConfig> 
        : IApiCallerCachingProxy<TConfig, ApiCallerCachingProxy<TApiDto, TCacheEntity, TConfig>>
        where TApiDto : class
        where TCacheEntity : class, IApiCallerCacheEntity<TApiDto, TCacheEntity>, new()
        where TConfig : ApiConfigurationValues<TConfig>
    {
        private readonly ILogger<IApiCaller<TConfig>> _logger;
        private readonly IRepository<TCacheEntity, CacheDbContext> _cacheRepo;
        private readonly IApiCaller<TConfig> _realApiCaller;

        public TConfig ConfigValues { get; }

        public ApiCallerCachingProxy(ILogger<IApiCaller<TConfig>> logger,
            IRepository<TCacheEntity, CacheDbContext> cacheRepo, IApiCaller<TConfig> realCaller,
            TConfig configValues)
        {
            _logger = logger;
            _cacheRepo = cacheRepo;
            _realApiCaller = realCaller;
            ConfigValues = configValues;
        }

        public async Task<TReturn> GetAsync<TReturn>(string url,
            params IEnumerable<KeyValuePair<string, string>>? queryParams)
            where TReturn : class
        {
            var queryList = queryParams?.ToList();
            var queryParamsStr = HttpHelperMethods.CreateQueryParamsFragment(queryList);
            TCacheEntity? cachedResult = null;
            try
            {
                // Precompute lowercase versions of the comparison strings to ensure EF Core can translate the comparisons to SQL
                var urlLower = (url ?? string.Empty).ToLowerInvariant();
                var queryParamsLower = (queryParamsStr ?? string.Empty).ToLowerInvariant();

                cachedResult = (await _cacheRepo.GetAllAsync(e =>
                        e.Url.ToLower() == urlLower
                        && e.QueryParams.ToLower() == queryParamsLower))
                    .FirstOrDefault();
                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for URL: {Url} with query params: {QueryParams}", url,
                        queryParams);
                }
                else
                {
                    _logger.LogInformation(
                        "Cache miss for URL: {Url} with query params: {QueryParams}. Calling real API.", url,
                        queryParams);
                    // Call the real API expecting the API DTO collection type
                    var apiResult = await _realApiCaller.GetAsync<IEnumerable<TApiDto>>(url, queryList);
                    // Cache the result (as a collection)
                    cachedResult = await CacheResults(url, apiResult, queryParamsStr);
                }

                // Determine how to return cached data based on requested return type
                // If caller expects the single DTO type TApiDto, return the first cached response
                if (typeof(TReturn) == typeof(TApiDto))
                {
                    var first = cachedResult.CachedResponses.FirstOrDefault();
                    return first as TReturn ?? throw new InvalidOperationException($"Cached entity for URL: {url} with query params: {queryParamsStr} has no cached responses.");
                }

                // If caller expects a collection of TApiDto (e.g., List<TApiDto> or IEnumerable<TApiDto>), return the collection
                if (typeof(TReturn).IsAssignableFrom(typeof(List<TApiDto>)) || typeof(IEnumerable<TApiDto>).IsAssignableFrom(typeof(TReturn)))
                {
                    return (object)cachedResult.CachedResponses as TReturn ?? throw new InvalidOperationException($"Cached entity for URL: {url} with query params: {queryParamsStr} could not be cast to the expected return type.");
                }

                // Fallback: try direct cast if types match otherwise fail
                return cachedResult as TReturn ?? throw new InvalidOperationException($"Cached entity for URL: {url} with query params: {queryParamsStr} could not be cast to the expected return type.");
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "An error occurred while mapping/converting JSON from a cached entity.");

                // If we had a cache hit but the cached data is invalid (e.g., due to a mapping error),
                // we should remove that cache entry to try and prevent future errors.
                if (cachedResult != null)
                {
                    await _cacheRepo.DeleteAsync(cachedResult);
                }

                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An unknow error occured during within {nameof(GetAsync)} of {GetType()}");
                throw;
            }
        }

        private async Task<TCacheEntity> CacheResults(string url, IEnumerable<TApiDto> apiResult, string? queryParamsStr)
        {
            try
            {
                // Create and populate cache entity manually to avoid static abstract/interface resolution issues at compile time.
                var cachedEntity = new TCacheEntity();
                // The constraint ensures TCacheEntity implements IApiCallerCacheEntity<TApiDto, TCacheEntity>
                var cacheAsInterface = (IApiCallerCacheEntity<TApiDto, TCacheEntity>)cachedEntity;
                cacheAsInterface.Url = url;
                cacheAsInterface.QueryParams = queryParamsStr ?? string.Empty;
                cacheAsInterface.CachedAt = DateTimeOffset.UtcNow;
                // Add the API results as the initial cached responses in the collection
                cacheAsInterface.CachedResponses = apiResult.ToList();

                return await _cacheRepo.AddOrUpdateAsync(cachedEntity);
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

    public interface IApiCallerCachingProxy<out TConfig, out TProxy> : IApiCaller<TConfig>
        where TConfig : ApiConfigurationValues<TConfig>
        where TProxy : class, IApiCallerCachingProxy<TConfig, TProxy>
    { }
}