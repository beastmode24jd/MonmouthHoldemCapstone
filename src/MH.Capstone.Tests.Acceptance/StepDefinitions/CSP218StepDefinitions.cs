using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP218StepDefinitions
{
    private readonly AuthenticationDriver _authDriver;
    private readonly ClubsDriver _clubsDriver;
    private readonly ChatroomDriver _chatroomDriver;

    // Shared across steps in a scenario.
    private string _chatroomUrl = string.Empty;

    public CSP218StepDefinitions(
        AuthenticationDriver authDriver,
        ClubsDriver clubsDriver,
        ChatroomDriver chatroomDriver)
    {
        _authDriver = authDriver;
        _clubsDriver = clubsDriver;
        _chatroomDriver = chatroomDriver;
    }

    [When("I navigate to the chatroom for my new club")]
    public void WhenINavigateToTheChatroomForMyNewClub()
    {
        // After CreateClub the browser is on /Clubs/ClubPage/{id}.
        // Click the "Go to Chatroom" button on that page.
        _chatroomDriver.NavigateToChatroomFromClubPage();
    }

    [Then("I should see the empty chatroom placeholder")]
    public void ThenIShouldSeeTheEmptyChatroomPlaceholder()
    {
        _chatroomDriver.IsOnChatroomPage().Should().BeTrue(
            "submitting the Create Club form and clicking 'Go to Chatroom' should land on the chatroom page");

        _chatroomDriver.IsEmptyStateVisible().Should().BeTrue(
            "a brand-new club has no messages so the empty-state placeholder should be shown");
    }

    [When(@"I send the message ""(.+)""")]
    public void WhenISendTheMessage(string message)
    {
        _chatroomDriver.SendMessage(message);
    }

    [Then(@"the message ""(.+)"" should appear in the chatroom")]
    public void ThenTheMessageShouldAppearInTheChatroom(string message)
    {
        _chatroomDriver.IsMessageVisible(message).Should().BeTrue(
            $"the message '{message}' should appear in the chatroom after being sent via SignalR");
    }

    [When("I note the chatroom URL for my new club")]
    public void WhenINoteTheChatroomUrlForMyNewClub()
    {
        // After WhenISelectPrivateForTheClub the browser is on /Clubs/ClubPage/{id}.
        _chatroomUrl = _chatroomDriver.GetChatroomUrl();
        TestContext.Out.WriteLine($"[CSP218] Noted chatroom URL: {_chatroomUrl}");
    }

    [When("I log in as Lily")]
    public void WhenILogInAsLily()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");
    }

    [When("I navigate directly to the noted chatroom URL")]
    public void WhenINavigateDirectlyToTheNotedChatroomUrl()
    {
        _chatroomDriver.NavigateToUrl(_chatroomUrl);
    }

    [Then("I should be denied access to the chatroom")]
    public void ThenIShouldBeDeniedAccessToTheChatroom()
    {
        _chatroomDriver.WasRedirectedAwayFromChatroom().Should().BeTrue(
            "Lily is not a member of Alex's private club and should be redirected away from the chatroom");
    }
}
