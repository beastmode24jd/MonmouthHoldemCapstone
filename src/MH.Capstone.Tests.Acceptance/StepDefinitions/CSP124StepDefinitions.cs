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

    // Scenario 1: James should not see the Clubs nav link

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

    // Scenario 2: Alex creates a new club

    [Given("I am on the Clubs page")]
    public void GivenIAmOnTheClubsPage()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        _clubsDriver.NavigateToLandingPage();
    }

    [When("I click the Create New Club button")]
    public void WhenIClickTheCreateNewClubButton()
    {
        // Scenario 3 calls this step AFTER "I select valid options" has already created
        // the club and redirected to the ClubPage — the create-club modal no longer
        // exists on that page. Guard so the step is a no-op in that ordering.
        if (_driver.Url.Contains("/Clubs/ClubPage/", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.Out.WriteLine("[CSP124] Skipping OpenCreateClubModal — already on ClubPage.");
            return;
        }
        _clubsDriver.OpenCreateClubModal();
    }

    [When("I select valid options")]
    public void WhenISelectValidOptions()
    {
        // Scenario 3 calls this step before "I click the Create New Club button",
        // so the modal may not be open yet — ensure it is before filling.
        _clubsDriver.EnsureCreateClubModalOpen();

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

    // Scenario 3: Alex makes a new club and invites Lily to it

    [Then("I should be able to invite another user")]
    public void ThenIShouldBeAbleToInviteAnotherUser()
    {
        
    }

    [Then("they should see the Club invite on their Clubs page")]
    public void ThenTheyShouldSeeTheClubInviteOnTheirClubsPage()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");
        _clubsDriver.NavigateToLandingPage();

        // Check for Alex's Club invite on Lily's Landing Page.
    }

    // Scenario 4: Alex creates a private club, Lily is not added as a member.

    [When("I select private for the Club")]
    public void WhenISelectPrivateForTheClub()
    {
        _clubsDriver.OpenCreateClubModal();

        // Use a unique suffix so re-runs do not conflict with clubs from prior runs.
        _newClubName = $"Acceptance Club {Guid.NewGuid().ToString()[..8]}";
        _clubsDriver.FillCreateClubModal(
            name: _newClubName,
            description: "Created by CSP-124 acceptance tests.",
            isPublic: false);

        _clubsDriver.SubmitCreateClubModal();
    }
    
    [When("do not add other users")]
    public void WhenDoNotAddOtherUsers()
    {
        _clubsDriver.IsOnClubPage().Should().BeTrue(
            "submitting the Create Club form should redirect to /Clubs/ClubPage/{id}");

        // Check club page for member list?
    }

    [Then("my Club should not be visible on Lily's Club page")]
    public void ThenMyClubShouldNotBeVisibleOnLilysClubPage()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");
        _clubsDriver.NavigateToLandingPage();

        // Check for Alex's Club on Lily's Landing Page.
        _clubsDriver.IsClubCardVisible(_newClubName).Should().BeFalse(
            $"the newly created club '{_newClubName}' should NOT appear under Lily's 'My Clubs'");
    }

    /*
    Scenario: Alex has a new club, and wants to add Lily to it.
        Given I am on the Clubs page -- DONE
        When I select valid options -- DONE
        And I click the Create New Club button -- DONE
        Then I should be able to invite another user
        And they should see the Club invite on their Clubs page

    Scenario: Alex has created a private club, and Lily is not added to it.
        Given I am on the Clubs page -- DONE
        When I select private for the Club -- DONE
        And do not add other users
        Then my Club should not be visible on Lily's Club page -- DONE
    */
}
