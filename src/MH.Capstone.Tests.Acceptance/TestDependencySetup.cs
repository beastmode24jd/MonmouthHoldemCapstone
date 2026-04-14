using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Hooks;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace MH.Capstone.Tests.Acceptance;

/// <summary>
/// Registers services into Reqnroll's per-scenario DI container.
///
/// Reqnroll discovers this class automatically because of the [ScenarioDependencies]
/// attribute on <see cref="CreateServices"/>. Concrete step-definition classes and
/// driver classes without an explicit registration are resolved automatically as
/// transient services by Reqnroll's container.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TestDependencySetup
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        // AcceptanceTestSettings is a singleton for the test run; the per-scenario
        // DI container gets the same instance that BeforeTestRun loaded.
        services.AddSingleton(Startup.GetSettings());

        // One shared ChromeDriver for all scenarios (avoids the cost of a browser
        // launch per scenario). Drivers and page objects reuse this instance.
        services.AddSingleton<IWebDriver>(Startup.GetWebDriver());

        // Driver classes must be explicitly registered when using
        // Reqnroll.Microsoft.Extensions.DependencyInjection — they are not
        // auto-discovered the way step-definition bindings are.
        services.AddTransient<AuthenticationDriver>();
        services.AddTransient<DashboardDriver>();
        services.AddTransient<SightingsDriver>();
        services.AddTransient<WildlifeSearchDriver>();

        return services;
    }
}
