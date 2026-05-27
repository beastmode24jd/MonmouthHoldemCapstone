using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Hooks;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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

        // One shared WebDriverWait for step definitions that need explicit waits.
        // Increased to 30s to reduce transient timing failures in CI/headless environments.
        services.AddSingleton(new WebDriverWait(Startup.GetWebDriver(), TimeSpan.FromSeconds(30)));

        // Driver classes must be explicitly registered when using
        // Reqnroll.Microsoft.Extensions.DependencyInjection — they are not
        // auto-discovered the way step-definition bindings are.
        services.AddTransient<AuthenticationDriver>();
        services.AddTransient<ClubsDriver>();
        services.AddTransient<DashboardDriver>();
        services.AddTransient<SightingGalleryDriver>();
        services.AddTransient<SightingsDriver>();
        services.AddTransient<SightingDetailsDriver>(); // CSP-172
        services.AddTransient<EditSightingDriver>(); // CSP-37
        services.AddTransient<WildlifeSearchDriver>();
        services.AddTransient<PasswordResetDriver>();
        services.AddTransient<EmailVerificationDriver>();
        services.AddTransient<UserSearchDriver>(); // Sprint 5, CSP-54
        services.AddTransient<NotificationsDriver>();
        services.AddTransient<DisplayNameDriver>();
        services.AddTransient<NotificationPreferencesDriver>();
        services.AddTransient<AccountSettingsDriver>();
        services.AddTransient<AnidexDriver>(); // CSP-142
        services.AddTransient<OfflineQueueDriver>(); // CSP-177
        services.AddTransient<LeaderboardDriver>(); // CSP-176
        services.AddTransient<LeaderboardLiveDriver>(); // CSP-180
        services.AddTransient<LiveNotificationSettingsDriver>(); // CSP-180
        services.AddTransient<FeedDriver>();     // CSP-187
        services.AddTransient<ProfileDriver>();  // CSP-187
        services.AddTransient<CommentsDriver>(); // CSP-187
        services.AddTransient<MapDriver>();      // CSP-217
        services.AddTransient<ChatroomDriver>(); // CSP-218
        services.AddTransient<AccountTabsDriver>(); // CSP-211

        return services;
    }
}
