using MH.Capstone.Tests.Acceptance.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP124StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AcceptanceTestSettings _settings;

    public CSP124StepDefinitions(IWebDriver driver, AcceptanceTestSettings settings)
    {
        _driver = driver;
        _settings = settings;
    }

    [Given("I am on the front page")]
    public void GivenIAmOnTheFrontPage()
    {
        // Should not be able to see Clubs in nav bar if not logged in.
        _driver.Navigate().GoToUrl(_settings.BaseUrl);
    }

    [When("I look at the nav bar")]
    public void WhenILookAtTheNavBar()
    {
        // No action needed; the nav bar is always visible after page load.
    }

    [Then("I should not see a Club page link")]
    public void ThenIShouldNotSeeAClubPageLink()
    {
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
        var clubLinks = _driver.FindElements(By.CssSelector("a[href='/Clubs']"));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        Assert.That(clubLinks, Is.Empty,
            "Unauthenticated users should not see the Clubs nav link");
    }

    /*
        Given I am on the front page
    When I look at the nav bar
    Then I should not see a Club page link
    */
}