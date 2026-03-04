using MH.Capstone.Domain.DataModels;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.WebApp
{
    [ExcludeFromCodeCoverage]
    internal static class Extensions
    {
        // TODO - Make this part of the configuration settings to allow for easier updates and
        // potential future changes to the default profile image.
        private const string DefaultProfileImageUrl = "/imgs/profileDefault.jpg";

        public static string GetProfileImageUrl(this ApplicationUser? user)
        {
            // Fetch the user profile image from the Model.
            // Defaults to the placeholder profile image if not found.
            // Convert byte[] to Base64 string for HTML display if custom image exists.
            return (user?.ProfileImage != null && !string.IsNullOrEmpty(user.ProfileImageType)) ?
                $"data:{user.ProfileImageType};base64,{Convert.ToBase64String(user.ProfileImage)}" :
                DefaultProfileImageUrl; // Default profile image URL for users without a custom profile image (or null user obj).
        }

        internal static async Task<int> GetPendingNotificationsCountAsync(this INotificationService service, ApplicationUser? user)
        {
            if (user == null)
            {
                return 0;
            }

            var tmp = await service.GetPendingNotificationsAsync(user);
            var pendingNotifications = tmp.ToList();
            return pendingNotifications.Count;

        }

        internal static TConfigVals GetApiConfig<TConfigVals>(this IConfigurationSection configSection, out string apiKey)
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            var endpointsSection = configSection.GetSection("Endpoints");
            if (!endpointsSection.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{configSection.Path}' is " +
                                                    $"missing required 'Endpoints' subsection.");
            }

            apiKey = configSection["ApiKey"] ?? throw new InvalidOperationException($"Configuration section " +
                $"'{configSection.Path}' is missing required 'ApiKey' value.");

            var configValues = configSection.Get<TConfigVals>();
            if (configValues == null)
            {
                throw new InvalidOperationException($"Configuration section '{configSection.Path}' could " +
                                                    $"not be bound to {typeof(TConfigVals).Name}.");
            }

            var endpoints = endpointsSection.GetChildren().Where(es =>
                !string.IsNullOrEmpty(es.Value)).Select(es =>
                new KeyValuePair<string, string>(es.Key, es.Value!)).ToList();

            return configValues.Create(endpointOverride: endpoints);
        }

        internal static TConfigVals Create<TConfigVals>(this TConfigVals current, 
            string? httpClientKetOverride = null, string? baseUrlOverride = null,
            List<KeyValuePair<string, string>>? endpointOverride = null)
            where TConfigVals : ApiConfigurationValues<TConfigVals>
        {
            httpClientKetOverride ??= current.HttpClientKey;
            baseUrlOverride ??= current.BaseUrl;
            endpointOverride ??= current.Endpoints;
           
            return current.Create(httpClientKetOverride, baseUrlOverride, endpointOverride);
        }
    }

    [ExcludeFromCodeCoverage]
    internal static class VersionHelper
    {
        public static string GetAppVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            var informationalVersion = assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return assembly?.GetName().Version?.ToString() ?? informationalVersion ?? "Version Unknown";
        }
    }
}
