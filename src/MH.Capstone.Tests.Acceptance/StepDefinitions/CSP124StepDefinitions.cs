using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP124StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AuthenticationDriver _authDriver;
    private readonly ClubsDriver _clubsDriver;

    // Shared across steps within this scenario; lets the Then steps know
    // which club name to look for without re-parsing the URL.
    private string _newClubName = string.Empty;

    public CSP124StepDefinitions(
        IWebDriver driver,
        AuthenticationDriver authDriver,
        ClubsDriver clubsDriver)
    {
        _driver = driver;
        _authDriver = authDriver;
        _clubsDriver = clubsDriver;
    }

    // -------------------------------------------------------------------------
    // Scenario 1: James (unauthenticated) should not see the Clubs nav link
    // -------------------------------------------------------------------------

    [Given("I am on the front page")]
    [When("I look at the nav bar")]
    public void GivenIAmOnTheFrontPage()
    {
        _authDriver.LogoutUser();
        _authDriver.WasPageAccessDenied(_driver.Url);   // ensure we're logged out
        _driver.Navigate().GoToUrl(_driver.Url.Split('/')[0] + "//" + _driver.Url.Split('/')[2]);
    }

    [Then("I should not see a Club page link")]
    public void ThenIShouldNotSeeAClubPageLink()
    {
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
        var clubLinks = _driver.FindElements(By.CssSelector("a[href='/Clubs']"));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        clubLinks.Should().BeEmpty("unauthenticated users should not see the Clubs nav link");
    }

    // -------------------------------------------------------------------------
    // Scenario 2: Alex creates a new club
    // -------------------------------------------------------------------------

    [Given("I am on the Clubs page")]
    public void GivenIAmOnTheClubsPage()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        _clubsDriver.NavigateToLandingPage();
    }

    [When("I click the Create New Club button")]
    public void WhenIClickTheCreateNewClubButton()
    {
        _clubsDriver.OpenCreateClubModal();
    }

    [When("I select valid options")]
    public void WhenISelectValidOptions()
    {
        // Use a unique suffix so re-runs do not conflict with clubs from prior runs.
        _newClubName = $"Acceptance Club {Guid.NewGuid().ToString()[..8]}";
        _clubsDriver.FillCreateClubModal(
            name: _newClubName,
            description: "Created by CSP-124 acceptance tests.",
            isPublic: true);

        _clubsDriver.SubmitCreateClubModal();
    }

    [Then("I should be redirected to my Club front page")]
    public void ThenIShouldBeRedirectedToMyClubFrontPage()
    {
        _clubsDriver.IsOnClubPage().Should().BeTrue(
            "submitting the Create Club form should redirect to /Clubs/ClubPage/{id}");

        // The page title should contain the club name.
        _driver.Title.Should().Contain(_newClubName,
            "the ClubPage title is set to the club's name");
    }

    [Then("see the new club on my Clubs page")]
    public void ThenSeeTheNewClubOnMyClubsPage()
    {
        _clubsDriver.NavigateToLandingPage();
        _clubsDriver.SwitchToMyClubsFilter();

        _clubsDriver.IsClubCardVisible(_newClubName).Should().BeTrue(
            $"the newly created club '{_newClubName}' should appear under 'My Clubs'");
    }
}
