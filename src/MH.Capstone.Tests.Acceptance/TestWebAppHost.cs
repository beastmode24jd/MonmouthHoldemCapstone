using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Seeding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

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
            EnvironmentName  = "Acceptance",
            ContentRootPath  = settings.WebAppContentRoot,
            // Must be the WebApp assembly name, not the NUnit testhost entry assembly.
            // ASP.NET Core's ApplicationPartManager walks this assembly's dependency
            // graph to discover controllers, Razor views, and tag helpers.  Without it,
            // every route returns 404 because no actions are registered.
            ApplicationName  = typeof(MH.Capstone.WebApp.Program).Assembly.GetName().Name,
        };

        var builder = WebApplication.CreateBuilder(options);

        // Bind Kestrel to 0.0.0.0 (all interfaces) on the port from AcceptanceTesting:BaseUrl.
        //
        // Using ConfigureKestrel() is intentional — it takes precedence over UseUrls() and
        // cannot be overridden by the ASPNETCORE_URLS environment variable.
        //
        // Binding to 0.0.0.0 rather than 127.0.0.1 is also intentional:
        //   • On WSL2, the test process runs in the Linux network namespace, but Chrome is
        //     typically the Windows browser.  WSL2 only auto-forwards ports bound to
        //     0.0.0.0 through to Windows localhost — ports bound to 127.0.0.1 are
        //     confined to the WSL2 loopback and are unreachable from Windows Chrome.
        //   • On a plain Windows or macOS machine 0.0.0.0 is equally fine; it just
        //     means Kestrel listens on every local interface instead of loopback-only.
        var baseUri = new Uri(settings.BaseUrl);
        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.Listen(System.Net.IPAddress.Any, baseUri.Port, listenOptions =>
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2));

        // WebApplication.CreateBuilder already loaded:
        //   appsettings.json + appsettings.Acceptance.json (via EnvironmentName above).
        // Layer in the optional per-developer local override file last, then env vars,
        // so they win over everything committed to source control.
        builder.Configuration
            .AddJsonFile(
                Path.Combine(settings.WebAppContentRoot, "appsettings.Acceptance.Local.json"),
                optional: true)
            .AddEnvironmentVariables()
            // Always enable the password-reset test endpoint so acceptance scenarios can
            // obtain reset links without a real email inbox.  This overrides any value set
            // in appsettings files and is safe because this host only runs during testing.
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:EnableEmailTestEndpoint"] = "true"
            });

        // MapStaticAssets() resolves the static-web-assets manifest by looking for
        // "{EntryAssemblyName}.staticwebassets.endpoints.json" next to the test binary.
        // The entry assembly under "dotnet test" is "testhost", but the manifest is
        // built and copied as "MH.Capstone.WebApp.staticwebassets.endpoints.json".
        // Copy it under the expected name so the middleware can find it.
        EnsureStaticAssetsManifest();

        // Route all WebApp log output (both startup and request-time) to Console.Out
        // so it appears in the NUnit test runner output alongside scenario results.
        var testOutputProvider = new TestOutputLoggerProvider();
        builder.Logging.AddProvider(testOutputProvider);

        // Build a startup logger factory using the same provider so that the Program.cs
        // entry logger (used before the DI container is built) also routes through it.
        var startupLoggerFactory = LoggerFactory.Create(b => b
            .AddProvider(testOutputProvider)
            .SetMinimumLevel(LogLevel.Information));

        _app = MH.Capstone.WebApp.Program.Configure(builder, startupLoggerFactory);
        startupLoggerFactory.Dispose();

        await _app.StartAsync();

        // Report configured URLs and run a TCP-level probe so failures are obvious.
        // The probe hits 127.0.0.1 (the Selenium navigation address), not 0.0.0.0.
        var boundUrls = string.Join(", ", _app.Urls);
        TestContext.Progress.WriteLine($"[TestWebAppHost] Kestrel bound:     0.0.0.0:{baseUri.Port}");
        TestContext.Progress.WriteLine($"[TestWebAppHost] Selenium base URL: {settings.BaseUrl}");
        await ProbePortAsync(baseUri.Host, baseUri.Port);

        // Wipe and re-seed the acceptance test database to a known state.
        // This runs once at the start of every test run, giving every scenario
        // a consistent baseline without needing to restart the application.
        try
        {
            await AcceptanceTestSeeder.SeedAsync(_app.Services);
            TestContext.Progress.WriteLine("[TestWebAppHost] Database seeded successfully.");
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[TestWebAppHost] SEEDER FAILED: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
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
    /// Opens a raw TCP connection to <paramref name="host"/>:<paramref name="port"/> and
    /// logs whether it succeeded.  A failure here means Kestrel did not actually bind —
    /// the cause will be in the WebApp log lines printed above.
    /// </summary>
    private static async Task ProbePortAsync(string host, int port)
    {
        using var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(host, port);
            TestContext.Progress.WriteLine($"[TestWebAppHost] TCP probe {host}:{port} → OPEN (Kestrel is accepting connections)");
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[TestWebAppHost] TCP probe {host}:{port} → FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies the WebApp's static-web-assets manifest to the name that
    /// <c>MapStaticAssets()</c> expects when the WebApp is hosted in-process
    /// under the <c>dotnet test</c> runner.
    /// <para>
    /// <c>MapStaticAssets()</c> resolves the manifest path from the entry
    /// assembly name, which under <c>dotnet test</c> is <c>testhost</c>.
    /// The manifest is built and output-copied as
    /// <c>MH.Capstone.WebApp.staticwebassets.endpoints.json</c>, so this method
    /// creates a copy named <c>testhost.staticwebassets.endpoints.json</c> in
    /// the same directory if it does not already exist.
    /// </para>
    /// </summary>
    private static void EnsureStaticAssetsManifest()
    {
        var outputDir  = AppContext.BaseDirectory;
        var sourceName = "MH.Capstone.WebApp.staticwebassets.endpoints.json";
        var targetName = "testhost.staticwebassets.endpoints.json";

        var source = Path.Combine(outputDir, sourceName);
        var target = Path.Combine(outputDir, targetName);

        if (File.Exists(source) && !File.Exists(target))
            File.Copy(source, target);
    }

    /// <summary>
    /// Wipes all application table rows and re-seeds the database to the exact
    /// same state that was established when the test run began.
    /// <para>
    /// Call this from a Reqnroll <c>[BeforeScenario]</c> hook whenever a scenario
    /// mutates database state (e.g. creates a sighting, resolves a report) and
    /// that mutation must not bleed into subsequent scenarios.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called before <see cref="StartAsync"/> has completed.
    /// </exception>
    public static async Task ResetSeedDataAsync()
    {
        if (_app is null)
            throw new InvalidOperationException(
                "Cannot reset seed data before the web application has been started. " +
                "Ensure StartAsync has been called (i.e. BeforeTestRun has executed).");

        await AcceptanceTestSeeder.SeedAsync(_app.Services);
    }
}
