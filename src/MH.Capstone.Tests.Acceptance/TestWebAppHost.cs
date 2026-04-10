using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MH.Capstone.Tests.Acceptance;

/// <summary>
/// Starts and stops an in-process instance of MH.Capstone.WebApp with a real Kestrel
/// HTTP listener, so Selenium can drive the application over the network just as a
/// user would — without needing to run the app in a separate terminal.
///
/// Configuration is loaded from the WebApp's appsettings hierarchy:
///   appsettings.json → appsettings.Acceptance.json → appsettings.Acceptance.Local.json
///
/// The app runs with ASPNETCORE_ENVIRONMENT = "Acceptance", so the WebApp picks up
/// the feature flags and connection strings defined in appsettings.Acceptance.json.
/// Per-developer overrides (ports, local DB strings, API keys) belong in
/// appsettings.Acceptance.Local.json (gitignored).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TestWebAppHost
{
    private static WebApplication? _app;

    public static async Task StartAsync(AcceptanceTestSettings settings)
    {
        if (_app is not null)
            return;

        var options = new WebApplicationOptions
        {
            EnvironmentName = "Acceptance",
            ContentRootPath = settings.WebAppContentRoot,
        };

        var builder = WebApplication.CreateBuilder(options);

        // Bind Kestrel to the URL configured in AcceptanceTesting:BaseUrl.
        // Developers override this in appsettings.Acceptance.Local.json if they need
        // a different port (e.g. to avoid a conflict with another running instance).
        builder.WebHost.UseUrls(settings.BaseUrl);

        // WebApplication.CreateBuilder already loaded:
        //   appsettings.json + appsettings.Acceptance.json (via EnvironmentName above).
        // Layer in the optional per-developer local override file last, then env vars,
        // so they win over everything committed to source control.
        builder.Configuration
            .AddJsonFile(
                Path.Combine(settings.WebAppContentRoot, "appsettings.Acceptance.Local.json"),
                optional: true)
            .AddEnvironmentVariables();

        _app = MH.Capstone.WebApp.Program.Configure(builder);
        await _app.StartAsync();
    }

    public static async Task StopAsync()
    {
        if (_app is null)
            return;

        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
    }

    /// <summary>
    /// Resets the database to a known seed state between test scenarios.
    /// Not yet implemented — implement when scenario isolation requires a clean DB.
    /// </summary>
    public static void ResetSeedData()
    {
        // TODO: inject ApplicationDbContext, delete non-seed rows, re-run SeedDataAsync
        throw new NotImplementedException("ResetSeedData is not yet implemented.");
    }
}
