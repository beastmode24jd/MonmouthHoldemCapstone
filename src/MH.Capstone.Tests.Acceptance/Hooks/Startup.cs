using System;
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Support;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Hooks;

[Binding]
[ExcludeFromCodeCoverage]
public sealed class Startup
{
    private static AcceptanceTestSettings? _settings;
    private static IWebDriver? _webDriver;

    /// <summary>
    /// Runs once before any test scenario.
    /// Loads configuration, starts the in-process WebApp, and opens a single
    /// shared browser session that all scenarios reuse.
    /// </summary>
    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _settings = AcceptanceTestConfiguration.Load();
        await TestWebAppHost.StartAsync(_settings);
        var baseUrl = TestWebAppHost.BoundUrl ?? _settings.BaseUrl;
        _webDriver = CreateWebDriver(_settings);
        _webDriver.Navigate().GoToUrl(baseUrl);
    }

    /// <summary>
    /// Runs once after all test scenarios have finished.
    /// Stops the WebApp and closes the browser.
    /// </summary>
    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await TestWebAppHost.StopAsync();

        _webDriver?.Quit();
        _webDriver?.Dispose();
        _webDriver = null;
    }

    /// <summary>
    /// Runs before every individual scenario.
    /// Navigates the shared browser to a neutral blank page so that every
    /// scenario begins with a clean browser state — no leftover form values,
    /// no stale JavaScript variables, and no URL that would accidentally
    /// satisfy a page-object "if not already there" guard inherited from a
    /// prior scenario.
    /// </summary>
    [BeforeScenario]
    public static void BeforeScenario()
    {
        _webDriver?.Navigate().GoToUrl("about:blank");
    }

    /// <summary>Returns the loaded test settings. Throws if called before BeforeTestRun.</summary>
    public static AcceptanceTestSettings GetSettings() =>
        _settings ?? throw new InvalidOperationException(
            "AcceptanceTestSettings have not been loaded. Ensure BeforeTestRun has executed.");

    /// <summary>Returns the shared WebDriver instance. Throws if called before BeforeTestRun.</summary>
    public static IWebDriver GetWebDriver() =>
        _webDriver ?? throw new InvalidOperationException(
            "IWebDriver has not been initialized. Ensure BeforeTestRun has executed.");

    private static IWebDriver CreateWebDriver(AcceptanceTestSettings settings)
    {
        var options = new ChromeOptions();

        if (settings.HeadlessSelenium)
        {
            options.AddArgument("--headless=new");
            // Ensure a consistent desktop viewport in headless mode so responsive layouts
            // and click targets match developer expectations.
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
        }

        // Required in CI/Docker environments
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        // Suppress TLS errors for the local dev certificate used by Kestrel
        options.AddArgument("--allow-insecure-localhost");

        var raw = new ChromeDriver(options);
        return new RobustWebDriver(raw, TimeSpan.FromSeconds(10));
    }
}
