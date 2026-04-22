using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Net;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Seeding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Tests.Acceptance.Support;

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

    public static string? BoundUrl { get; private set; }

    public static async Task StartAsync(AcceptanceTestSettings settings)
    {
        if (_app is not null)
            return;

        var baseUri = new Uri(settings.BaseUrl);
        var initialPort = baseUri.Port;
        int portToTry = initialPort;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            var options = new WebApplicationOptions
            {
                EnvironmentName  = "Acceptance",
                ContentRootPath  = settings.WebAppContentRoot,
                ApplicationName  = typeof(MH.Capstone.WebApp.Program).Assembly.GetName().Name,
            };

            var builder = WebApplication.CreateBuilder(options);

            // Configure Kestrel to listen on the chosen port for this attempt.
            builder.WebHost.ConfigureKestrel(kestrel =>
                kestrel.Listen(System.Net.IPAddress.Any, portToTry, listenOptions =>
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2));

            builder.Configuration
                .AddJsonFile(
                    Path.Combine(settings.WebAppContentRoot, "appsettings.Acceptance.Local.json"),
                    optional: true)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:EnableEmailTestEndpoint"] = "true"
                });

            builder.Services.AddSingleton<IAIService, TestAIService>();
            EnsureStaticAssetsManifest();

            var runtimeProvider = new TestOutputLoggerProvider();
            builder.Logging.AddProvider(runtimeProvider);

            var startupLoggerFactory = LoggerFactory.Create(b => b
                .AddProvider(new TestOutputLoggerProvider())
                .SetMinimumLevel(LogLevel.Information));

            _app = MH.Capstone.WebApp.Program.Configure(builder, startupLoggerFactory);
            startupLoggerFactory.Dispose();

            try
            {
                await _app.StartAsync();

                BoundUrl = $"http://127.0.0.1:{portToTry}";
                TestContext.Progress.WriteLine($"[TestWebAppHost] Kestrel bound:     0.0.0.0:{portToTry}");
                TestContext.Progress.WriteLine($"[TestWebAppHost] Selenium base URL: {BoundUrl}");

                // Probe the actual bound address so failures are obvious
                await ProbePortAsync("127.0.0.1", portToTry);

                // Wipe and re-seed the acceptance test database.
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

                // Successful start
                return;
            }
            catch (Exception ex)
            {
                // Determine if this failure looks like an address-in-use error and retry once
                var isAddressInUse = ex is SocketException ||
                                     ex.InnerException is SocketException ||
                                     (ex.Message?.IndexOf("address", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                      ex.Message?.IndexOf("in use", StringComparison.OrdinalIgnoreCase) >= 0);

                TestContext.Progress.WriteLine($"[TestWebAppHost] Start attempt for port {portToTry} failed: {ex.GetType().Name}: {ex.Message}");

                if (isAddressInUse && attempt == 0)
                {
                    TestContext.Progress.WriteLine($"[TestWebAppHost] Port {portToTry} appears in use; selecting a free port and retrying.");

                    try
                    {
                        await _app.StopAsync();
                        await _app.DisposeAsync();
                    }
                    catch { }
                    _app = null;

                    // Pick a free port by letting the OS assign one
                    var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
                    listener.Start();
                    portToTry = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                    listener.Stop();

                    continue; // retry with new port
                }

                // Not recoverable or already retried
                throw;
            }
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
