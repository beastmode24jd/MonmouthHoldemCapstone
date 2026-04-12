using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Reqnroll.BoDi;

namespace MH.Capstone.Tests.Acceptance.Support;

/// <summary>
/// Shared Selenium WebDriver lifecycle for any feature tagged @ai-companion
/// (and future stories such as CSP-153 once they opt in to the same tag).
/// Registers a singleton IWebDriver + WebDriverWait into the scenario's
/// Reqnroll DI container so step definition classes can receive them via
/// constructor injection rather than spinning up their own driver each scenario.
/// </summary>
[Binding]
public class Hooks
{
    public const string BaseUrl = "https://localhost:7147";

    private readonly IObjectContainer _container;

    public Hooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario("@ai-companion")]
    public void RegisterHeadlessChromeDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

        _container.RegisterInstanceAs<IWebDriver>(driver);
        _container.RegisterInstanceAs(wait);
    }

    [AfterScenario("@ai-companion")]
    public void DisposeDriver()
    {
        if (_container.IsRegistered<IWebDriver>())
        {
            var driver = _container.Resolve<IWebDriver>();
            driver?.Quit();
            driver?.Dispose();
        }
    }
}
