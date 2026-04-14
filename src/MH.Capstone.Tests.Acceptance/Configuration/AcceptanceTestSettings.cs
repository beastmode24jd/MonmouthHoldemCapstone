using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Tests.Acceptance.Configuration;

/// <summary>
/// Configuration settings for the acceptance test runner.
/// Loaded from the WebApp's appsettings.Acceptance.json / appsettings.Acceptance.Local.json
/// under the "AcceptanceTesting" section.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AcceptanceTestSettings
{
    /// <summary>Base URL Selenium navigates to (e.g. http://localhost:7654).</summary>
    public string BaseUrl { get; init; } = "http://localhost:7654";

    /// <summary>When true, Chrome runs headless (no visible window).</summary>
    public bool HeadlessSelenium { get; init; } = true;

    /// <summary>Absolute path to the MH.Capstone.WebApp project directory; resolved at load time.</summary>
    public string WebAppContentRoot { get; init; } = string.Empty;
}
