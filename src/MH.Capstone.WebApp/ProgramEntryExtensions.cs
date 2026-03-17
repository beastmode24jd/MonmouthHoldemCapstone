using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MH.Capstone.WebApp
{
    [ExcludeFromCodeCoverage]
    internal static class ProgramEntryExtensions
    {
        public static ILoggingBuilder ConfigureLogging(this ILoggingBuilder builder, IWebHostEnvironment env)
        {
            // Configure Logging, with some based on environment
            // Note: DO NOT REMOVE THE CONSOLE LOGGER OR AZURE.
            // Azure App Service relies on it for log collection.
            builder.AddConsole();
            if (!env.IsDevelopment())
            {
                // Staging or Production - add Azure App Service diagnostics logging
                builder.AddAzureWebAppDiagnostics();
            }

            return builder;
        }

        public static IServiceCollection AddExternalApiCaller<TConfigVals>(
            this IServiceCollection services, IConfiguration config,
            IWebHostEnvironment env, ILogger logger,
            string configSectionPath, ApiCallerOptions callerOpts)
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            var configSection = config.GetSection(configSectionPath);
            
            if (!configSection.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{configSectionPath}' is missing.");
            }

            var configVals = configSection.GetApiConfig<TConfigVals>(out var apiKey)
                .EnsureValid(apiKey, env);
            logger.LogInformation($"{typeof(TConfigVals)} obtained and ensured valid from configuration.");
            return services.AddExternalApiCaller(logger, configVals, apiKey, callerOpts);
        }

        private static IServiceCollection AddExternalApiCaller<TConfigVals>(
            this IServiceCollection services, ILogger logger, TConfigVals configVals,
            string apiKey, ApiCallerOptions callerOpts) where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            // Configure HttpClient for external API calls (e.g., AnimalApi, Emailer, etc.)
            services.AddHttpClient(configVals.HttpClientKey, client =>
            {
                // BaseAddress and other settings can be configured when injecting the client
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                client.BaseAddress = new Uri(configVals.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15); // Set a reasonable timeout
            }).AsExternalApiCaller(logger, configVals, callerOpts);

            logger.LogInformation($"Completed setup and DI registration of {nameof(ExternalApiCaller<TConfigVals>)}.");
            return services;
        }

        private static IHttpClientBuilder AsExternalApiCaller<TConfigVals>(
            this IHttpClientBuilder builder, ILogger logger,
            TConfigVals config, ApiCallerOptions options)
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            //builder.Services.AddSingleton(config);
            Func<IServiceProvider, ExternalApiCaller<TConfigVals>> realCallerFac = services =>
                new ExternalApiCaller<TConfigVals>(
                    services.GetRequiredService<ILogger<IApiCaller<TConfigVals>>>(),
                    services.GetRequiredService<IHttpClientFactory>(), config);

            if (options.IsCacheProxyConfigured)
            {
                logger.LogInformation($"{nameof(ApiCallerOptions)} with call to {nameof(AsExternalApiCaller)} " +
                                      $"configured to use proxy type {options.CacheProxyType}.");
                

            }
            else
            {
                logger.LogInformation($"{nameof(ApiCallerOptions)} with call to {nameof(AsExternalApiCaller)} " +
                                      $"configured to use real {nameof(ExternalApiCaller<TConfigVals>)}.");
                builder.Services.AddScoped<IApiCaller<TConfigVals>, ExternalApiCaller<TConfigVals>>(realCallerFac);
            }

            return builder;
        }
    }

    [ExcludeFromCodeCoverage]
    internal class ApiCallerOptions
    {
        private readonly ApiCallerOptions _options;

        public bool IsCacheProxyConfigured => CacheProxyType != null;

        public Type? CacheProxyType { get; private set; } = null;

        public static ApiCallerOptions Default => new ApiCallerOptions();

        public ApiCallerOptions()
        {
            _options = this;
        }

        public ApiCallerOptions UseCacheProxy<TConfig, TProxy>()
            where TConfig : ApiConfigurationValues<TConfig>
            where TProxy : class, IApiCallerCachingProxy<TConfig, TProxy>
        {
            CacheProxyType = typeof(TProxy);
            return this;
        }
    }
}
