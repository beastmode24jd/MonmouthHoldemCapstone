using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class DashboardDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _dashboardUrl;

    public DashboardDriver(IWebDriver webDriver, AcceptanceTestSettings settings)
    {
        _webDriver = webDriver;
        _dashboardUrl = $"{settings.BaseUrl.TrimEnd('/')}/Dashboard";
    }

    public bool IsOnDashboard()
    {
        if (string.Equals(_webDriver.Url, _dashboardUrl, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        TestContext.Out.WriteLine($"[{nameof(DashboardDriver.IsOnDashboard)}] URL is not on the dashboard, but at {_webDriver.Url}.");
        return false;
    }
        
}
