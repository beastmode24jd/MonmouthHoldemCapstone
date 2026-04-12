using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Reqnroll.BoDi;

namespace MH.Capstone.Tests.Acceptance.Support;

/// <summary>
/// Shared Selenium WebDriver for BDD test. Removes duplicate browers setup code. 
/// Centalized Chrome driver creation and tear down. Uses headless Chrome for CI compatibility.

/// To add a new feature:
///   1. Add @selenium to the .feature file
///   2. Accept IWebDriver + WebDriverWait in the step def constructor
///   3. That's it — Hooks handles setup and teardown
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

    [BeforeScenario("@selenium", "@ai-companion")]
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

    [AfterScenario("@selenium", "@ai-companion")]
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
