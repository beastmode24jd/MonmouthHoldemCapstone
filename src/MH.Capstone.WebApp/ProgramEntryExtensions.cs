using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.WebApp
{
    [ExcludeFromCodeCoverage]
    internal static class ProgramEntryExtensions
    {
        public static IServiceCollection AddExternalApiCaller<TConfigVals>(
            this IServiceCollection services, IWebHostEnvironment env,
            IConfiguration config, string configSectionPath, ApiCallerOptions callerOpts)
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            var configSection = config.GetSection(configSectionPath);
            
            if (!configSection.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{configSectionPath}' is missing.");
            }

            var configVals = configSection.GetApiConfig<TConfigVals>(out var apiKey).EnsureValid(apiKey, env);
            return services.AddExternalApiCaller(configVals, apiKey, callerOpts);
        }

        public static IHttpClientBuilder AsExternalApiCaller<TConfigVals>(
            this IHttpClientBuilder builder, TConfigVals config, ApiCallerOptions options) 
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            //builder.Services.AddSingleton(config);
            Func<IServiceProvider, ExternalApiCaller<TConfigVals>> realCallerFac = services => 
                new ExternalApiCaller<TConfigVals>(
                services.GetRequiredService<ILogger<IApiCaller<TConfigVals>>>(),
                services.GetRequiredService<IHttpClientFactory>(), config);

            if (options.CacheProxyConfigured)
            {
                builder.Services.AddScoped<IApiCaller<TConfigVals>,
                    ApiCallerCachingProxy<ApiCallerCacheEntity, TConfigVals>>(services =>
                    new ApiCallerCachingProxy<ApiCallerCacheEntity, TConfigVals>(
                        services.GetRequiredService<ILogger<ApiCallerCachingProxy<ApiCallerCacheEntity, TConfigVals>>>(),
                        services.GetRequiredService<IRepository<ApiCallerCacheEntity, ApplicationDbContext>>(),
                        realCallerFac.Invoke(services), config));
            }
            else
            {
                builder.Services.AddScoped<IApiCaller<TConfigVals>, ExternalApiCaller<TConfigVals>>(realCallerFac);
            }

            return builder;
        }

        private static IServiceCollection AddExternalApiCaller<TConfigVals>(
            this IServiceCollection services, TConfigVals configVals, string apiKey,
            ApiCallerOptions callerOpts) where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            // Configure HttpClient for external API calls (e.g., AnimalApi, Emailer, etc.)
            services.AddHttpClient(configVals.HttpClientKey, client =>
            {
                // BaseAddress and other settings can be configured when injecting the client
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                client.BaseAddress = new Uri(configVals.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15); // Set a reasonable timeout
            }).AsExternalApiCaller(configVals, callerOpts);

            return services;
        }
    }

    [ExcludeFromCodeCoverage]
    internal class ApiCallerOptions
    {
        private readonly ApiCallerOptions _options = Default;

        public bool CacheProxyConfigured { get; set; } = false;

        public static ApiCallerOptions Default => new ApiCallerOptions();

        public ApiCallerOptions UseCacheProxy()
        {
            _options.CacheProxyConfigured = true;
            return _options;
        }
    }
}
