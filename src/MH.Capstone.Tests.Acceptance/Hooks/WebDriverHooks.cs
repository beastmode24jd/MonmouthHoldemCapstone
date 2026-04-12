using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Support;

[Binding]
public class WebDriverHooks
{
    // Generated using gemini
    private readonly ScenarioContext _scenarioContext;

    public WebDriverHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario("@dashboard")]
    public void BeforeScenario()
    {
        ChromeOptions options = new ChromeOptions();

        // Headless browser flags
        options.AddArgument("--headless=new"); // Modern headless mode
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        // Initialize Chrome (Selenium Manager handles the driver binary automatically)
        IWebDriver driver = new ChromeDriver();
        _scenarioContext["WebDriver"] = driver;
    }

    [AfterScenario("@dashboard")]
    public void AfterScenario()
    {
        if (_scenarioContext.TryGetValue("WebDriver", out IWebDriver driver))
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}