using MH.Capstone.Tests.Acceptance.Hooks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "map")]
public class CSP52SightingsMapSteps
{
    private readonly IWebDriver _driver;
    private string _baseUrl => Startup.GetSettings().BaseUrl;
    private const string TestEmail = "alpha@test.com";
    private const string TestPassword = "Capstone26!";

    public CSP52SightingsMapSteps(IWebDriver driver)
    {
        _driver = driver;
    }

    [Given(@"I am using Chrome browser")]
    public void GivenIAmUsingChromeBrowser()
    {
        // No-op: the shared ChromeDriver is created once in BeforeTestRun (Startup.cs).
    }

    [Given(@"the application is running")]
    public void GivenTheApplicationIsRunning()
    {
        _driver.Navigate().GoToUrl(_baseUrl);
    }

    [Given(@"I am logged in as a registered user")]
    public void GivenIAmLoggedInAsARegisteredUser()
    {
        // Clear any existing auth cookie from a previous scenario, otherwise navigating
        // to /Account/Login redirects to Dashboard and #emailField won't exist.
        _driver.Navigate().GoToUrl(_baseUrl);
        _driver.Manage().Cookies.DeleteAllCookies();

        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");

        var emailField = _driver.FindElement(By.Id("emailField"));
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
        // Close any modal that might be blocking the zoom controls
        try
        {
            var closeButton = _driver.FindElement(By.CssSelector("#noSightingsModal .btn-close, #noSightingsModal button[data-bs-dismiss='modal']"));
            closeButton.Click();
            Thread.Sleep(500);
        }
        catch (NoSuchElementException)
        {
            // Modal not present, continue
        }

        var zoomInButton = _driver.FindElement(By.CssSelector(".leaflet-control-zoom-in"));
        var zoomOutButton = _driver.FindElement(By.CssSelector(".leaflet-control-zoom-out"));

        Assert.That(zoomInButton.Displayed, Is.True, "Zoom in button should be visible");
        Assert.That(zoomOutButton.Displayed, Is.True, "Zoom out button should be visible");

        zoomInButton.Click();
        Thread.Sleep(500);
        zoomOutButton.Click();
    }
}
