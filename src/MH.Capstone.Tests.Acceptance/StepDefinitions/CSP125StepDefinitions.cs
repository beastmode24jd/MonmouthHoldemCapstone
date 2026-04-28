using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP125StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AuthenticationDriver _authDriver;
    private readonly ClubsDriver _clubsDriver;

    // Shared across steps within this scenario; lets the Then steps know
    // which club name to look for without re-parsing the URL.
    private string _newClubName = string.Empty;

    public CSP125StepDefinitions(
        IWebDriver driver,
        AuthenticationDriver authDriver,
        ClubsDriver clubsDriver)
    {
        _driver = driver;
        _authDriver = authDriver;
        _clubsDriver = clubsDriver;
    }

    // Scenario 1: Alex is on his front Club page

    [Given("I am on the Club front page")]
    public void GivenIAmOnTheClubFrontPage()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        _clubsDriver.NavigateToLandingPage();
    }

    [When("I look under the Club name and description")]
    public void WhenILookUnderTheClubNameAndDescription()
    {
        
    }

    [Then("I should see the newest Sightings from the other Club members")]
    public void ThenIShouldSeeTheNewestSightingsFromTheOtherClubMembers()
    {
        
    }

    [Then("the front page should update if I upload a new Sighting")]
    public void ThenTheFrontPageShouldUpdateIfIUploadANewSighting()
    {
        // Upload a sighting, then,

        _clubsDriver.NavigateToLandingPage();
    }

    // Scenario 2: Lily leaves the Club, and Alex is on the front Club page

    [Given("Lily leaves the Club")]
    public void GivenLilyLeavesTheClub()
    {
        
    }

    [Then("I should see it update to remove Lily Sightings from the feed")]
    public void ThenIShouldSeeItUpdateToRemoveLilySightingsFromTheFeed()
    {
        
    }

    /*
        Scenario: Alex is on his front Club page
            Given I am on the Club front page
            When I look under the Club name and description
            Then I should see the newest Sightings from the other Club members
            And the front page should update if I upload a new Sighting

        Scenario: Lily leaves the Club, and Alex is on the front Club page
            Given I am on the Club front page
            And Lily leaves the Club
            When I look under the Club name and description
            Then I should see it update to remove Lily Sightings from the feed

    */

}