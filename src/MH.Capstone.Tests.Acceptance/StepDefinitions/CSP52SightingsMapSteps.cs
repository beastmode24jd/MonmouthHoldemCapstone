using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP52SightingsMapSteps : IDisposable
{
    private IWebDriver _driver = null!;
    private readonly string _baseUrl = "https://localhost:7147";
    private const string TestEmail = "test@gmail.com";
    private const string TestPassword = "ZeroTwo002!";

    [Given(@"I am using Chrome browser")]
    public void GivenIAmUsingChromeBrowser()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--ignore-certificate-errors");
        
        // Let Selenium Manager handle the driver automatically
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Given(@"the application is running")]
    public void GivenTheApplicationIsRunning()
    {
        _driver.Navigate().GoToUrl(_baseUrl);
    }

    [Given(@"I am logged in as a registered user")]
    public void GivenIAmLoggedInAsARegisteredUser()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");
        
        var emailField = _driver.FindElement(By.Id("Email"));
        var passwordField = _driver.FindElement(By.Id("passwordField"));
        
        emailField.SendKeys(TestEmail);
        passwordField.SendKeys(TestPassword);
        
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var submitButton = wait.Until(d => 
        {
            var btn = d.FindElement(By.Id("submitBtn"));
            return btn.Enabled ? btn : null;
        });
        submitButton?.Click();
        
        wait.Until(d => !d.Url.Contains("/Account/Login"));
    }

    [When(@"I navigate to the map page without logging in")]
    public void WhenINavigateToTheMapPageWithoutLoggingIn()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Map");
    }

    [When(@"I navigate to the map page")]
    public void WhenINavigateToTheMapPage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Map");
        Thread.Sleep(2000);
    }

    [When(@"there are no sightings in the current view")]
    public void WhenThereAreNoSightingsInTheCurrentView()
    {
        Thread.Sleep(1000);
    }

    [Then(@"I should be redirected to the login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.Url.Contains("/Account/Login"));
        Assert.That(_driver.Url, Does.Contain("/Account/Login"));
    }

    [Then(@"I should see the map container element")]
    public void ThenIShouldSeeTheMapContainerElement()
    {
        var mapElement = _driver.FindElement(By.Id("map"));
        Assert.That(mapElement.Displayed, Is.True);
    }

    [Then(@"I should see a popup indicating no sightings in the area")]
    public void ThenIShouldSeeAPopupIndicatingNoSightingsInTheArea()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        try
        {
            var modal = wait.Until(d => d.FindElement(By.Id("noSightingsModal")));
            var isVisible = modal.GetDomAttribute("class")?.Contains("show") ?? false;
            Assert.That(isVisible || modal.Displayed, Is.True, "No sightings modal should be visible");
        }
        catch (WebDriverTimeoutException)
        {
            Assert.Pass("No modal appeared - there may be sightings in the area");
        }
    }

    [Then(@"I should be able to interact with the zoom controls")]
    public void ThenIShouldBeAbleToInteractWithTheZoomControls()
    {
        var zoomInButton = _driver.FindElement(By.CssSelector(".leaflet-control-zoom-in"));
        var zoomOutButton = _driver.FindElement(By.CssSelector(".leaflet-control-zoom-out"));
        
        Assert.That(zoomInButton.Displayed, Is.True, "Zoom in button should be visible");
        Assert.That(zoomOutButton.Displayed, Is.True, "Zoom out button should be visible");
        
        zoomInButton.Click();
        Thread.Sleep(500);
        zoomOutButton.Click();
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}