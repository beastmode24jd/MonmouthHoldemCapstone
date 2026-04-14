using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace MH.Capstone.Tests.Acceptance.Configuration;

/// <summary>
/// Loads acceptance-test configuration from the WebApp project's appsettings hierarchy.
///
/// Config load order (later entries override earlier ones):
///   1. MH.Capstone.WebApp/appsettings.json          – base defaults
///   2. MH.Capstone.WebApp/appsettings.Acceptance.json – environment defaults (committed)
///   3. MH.Capstone.WebApp/appsettings.Acceptance.Local.json – per-developer overrides (gitignored, optional)
///   4. Environment variables                          – CI/CD overrides
///
/// To provide developer-specific settings (connection strings, ports, API keys), create
/// MH.Capstone.WebApp/appsettings.Acceptance.Local.json — it is already covered by the
/// repository's gitignore rule and will never be committed.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class AcceptanceTestConfiguration
{
    public static AcceptanceTestSettings Load()
    {
        var webAppRoot = FindWebAppContentRoot();

        var config = new ConfigurationBuilder()
            .SetBasePath(webAppRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Acceptance.json", optional: true)
            .AddJsonFile("appsettings.Acceptance.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new AcceptanceTestSettings
        {
            BaseUrl         = config["AcceptanceTesting:BaseUrl"]         ?? "http://localhost:7654",
            HeadlessSelenium = bool.Parse(config["AcceptanceTesting:HeadlessSelenium"] ?? "true"),
            WebAppContentRoot = webAppRoot,
        };
    }

    /// <summary>
    /// Walks up the directory tree from the test binary's output folder to locate
    /// the MH.Capstone.WebApp source directory. Works on developer machines and in CI
    /// because both keep the same relative source layout.
    /// </summary>
    private static string FindWebAppContentRoot()
    {
        const string webAppProjectName = "MH.Capstone.WebApp";
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, webAppProjectName);
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Cannot locate the '{webAppProjectName}' project directory. " +
            $"Searched upward from: {AppContext.BaseDirectory}. " +
            $"Ensure the solution structure has not changed.");
    }
}
