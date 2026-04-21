using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP138StepDefinitions
{
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly NotificationsDriver _notificationsDriver;

    public CSP138StepDefinitions(
        AuthenticationDriver authenticationDriver,
        NotificationsDriver notificationsDriver)
    {
        _authenticationDriver = authenticationDriver;
        _notificationsDriver = notificationsDriver;
    }

    // ── Shared Given steps ────────────────────────────────────────────────────

    [Given("Alex is logged in and views their notifications")]
    public void GivenAlexIsLoggedInAndViewsTheirNotifications()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", AcceptanceTestSeeder.TestPassword);
        _notificationsDriver.NavigateToNotifications();
    }

    [Given("Patricia is logged in and views their notifications")]
    public void GivenPatriciaIsLoggedInAndViewsTheirNotifications()
    {
        _authenticationDriver.PreformLoginForUser("patricia@test.com", AcceptanceTestSeeder.TestPassword);
        _notificationsDriver.NavigateToNotifications();
    }

    [Given("Alex has unread notifications")]
    public void GivenAlexHasUnreadNotifications()
    {
        _notificationsDriver.HasUnreadNotifications().Should().BeTrue(
            "Alex is seeded with unread notifications");
    }

    [Given("Alex has notifications")]
    public void GivenAlexHasNotifications()
    {
        _notificationsDriver.HasAnyNotifications().Should().BeTrue(
            "Alex is seeded with notifications");
    }

    [Given("Alex has no notifications remaining")]
    public void GivenAlexHasNoNotificationsRemaining()
    {
        // Log in as Alex, delete all his notifications, then we can verify the suppressed state
        _authenticationDriver.PreformLoginForUser("alex@test.com", AcceptanceTestSeeder.TestPassword);
        _notificationsDriver.NavigateToNotifications();
        _notificationsDriver.HasAnyNotifications().Should().BeTrue(
            "Alex must have notifications in seed data before deleting them");
        _notificationsDriver.ClickDeleteAll();
        // After delete all the page reloads; navigate back to verify the suppressed state
        _notificationsDriver.NavigateToNotifications();
    }

    // ── When steps ────────────────────────────────────────────────────────────

    [When("Alex selects Mark All as Read")]
    public void WhenAlexSelectsMarkAllAsRead()
    {
        _notificationsDriver.ClickMarkAllRead();
    }

    [When("Alex selects Delete All")]
    public void WhenAlexSelectsDeleteAll()
    {
        _notificationsDriver.ClickDeleteAll();
    }

    [When("Alex views their notifications")]
    public void WhenAlexViewsTheirNotifications()
    {
        _notificationsDriver.NavigateToNotifications();
    }

    // ── Then steps ────────────────────────────────────────────────────────────

    [Then("all notifications are marked as read")]
    public void ThenAllNotificationsAreMarkedAsRead()
    {
        _notificationsDriver.HasUnreadNotifications().Should().BeFalse(
            "all notifications should be read after Mark All as Read");
    }

    [Then("the Mark All as Read control is not visible")]
    public void ThenTheMarkAllAsReadControlIsNotVisible()
    {
        _notificationsDriver.IsMarkAllReadVisible().Should().BeFalse(
            "Mark All as Read should be hidden when there are no unread notifications");
    }

    [Then("the notification count badge is cleared")]
    public void ThenTheNotificationCountBadgeIsCleared()
    {
        _notificationsDriver.IsNotificationBadgeClear().Should().BeTrue(
            "the notification badge should show zero unread after a bulk read or delete");
    }

    [Then("the notification list displays an empty state message")]
    public void ThenTheNotificationListDisplaysAnEmptyStateMessage()
    {
        _notificationsDriver.IsEmptyStateVisible().Should().BeTrue(
            "an empty state message should be shown when there are no notifications");
    }

    [Then("the Delete All control is not visible")]
    public void ThenTheDeleteAllControlIsNotVisible()
    {
        _notificationsDriver.IsDeleteAllVisible().Should().BeFalse(
            "Delete All should be hidden when there are no notifications");
    }

    [Then("only Alex's notifications are deleted")]
    public void ThenOnlyAlexsNotificationsAreDeleted()
    {
        _notificationsDriver.IsEmptyStateVisible().Should().BeTrue(
            "Alex should have no notifications after deleting all");
    }

    [Then("Lily's notifications remain unchanged")]
    public void ThenLilysNotificationsRemainUnchanged()
    {
        _authenticationDriver.PreformLoginForUser("lily@test.com", AcceptanceTestSeeder.TestPassword);
        _notificationsDriver.NavigateToNotifications();

        _notificationsDriver.HasAnyNotifications().Should().BeTrue(
            "Lily's notifications should be unaffected by Alex's bulk delete");
    }

}
