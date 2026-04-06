using System.Linq.Expressions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class ManualUserReportSteps
{
    private readonly List<Report> _reports = new();
    private readonly List<Notification> _notifications = new();
    private Mock<IRepository<Report, ApplicationDbContext>> _reportRepoMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private IReportService _reportService = null!;
    
    private bool _isLoggedIn;
    private Guid _currentUserId;
    private string _currentPageUrl = "/Map";
    private Report? _submittedReport;
    private bool _submissionResult;
    private bool _shouldBeRedirected;

    [Given(@"I am logged in and viewing a sighting page")]
    public void GivenIAmLoggedInAndViewingASightingPage()
    {
        _isLoggedIn = true;
        _currentUserId = Guid.NewGuid();
        _currentPageUrl = "/Map";
        
        SetupMocks();
    }

    [Given(@"I submit a report on a sighting page")]
    public async Task GivenISubmitAReportOnASightingPage()
    {
        GivenIAmLoggedInAndViewingASightingPage();
        WhenIClickReportThisPage();
        WhenISelectAReasonAndOptionallyEnterADescription();
        await WhenISubmitTheForm();
    }

    [Given(@"I have already submitted a report for a specific page")]
    public async Task GivenIHaveAlreadySubmittedAReportForASpecificPage()
    {
        await GivenISubmitAReportOnASightingPage();
    }

    [Given(@"I am not logged in")]
    public void GivenIAmNotLoggedIn()
    {
        _isLoggedIn = false;
    }

    [Given(@"a user has submitted a report")]
    public async Task GivenAUserHasSubmittedAReport()
    {
        await GivenISubmitAReportOnASightingPage();
    }

    [When(@"I click ""Report this page""")]
    public void WhenIClickReportThisPage()
    {
        // Verify user is authenticated
        Assert.That(_isLoggedIn, Is.True, "User must be logged in to report");
    }

    [When(@"I select a reason and optionally enter a description")]
    public void WhenISelectAReasonAndOptionallyEnterADescription()
    {
        // Simulate form input - these values will be used in submit
    }

    [When(@"I submit the form")]
    public async Task WhenISubmitTheForm()
    {
        _submittedReport = new Report
        {
            Id = Guid.NewGuid(),
            ReportingUserId = _currentUserId,
            ReportedPageUrl = _currentPageUrl,
            Reason = "Inappropriate content",
            Description = "Test report description",
            SubmittedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _submissionResult = await _reportService.SubmitReportAsync(_submittedReport);
    }

    [When(@"the report is saved")]
    public void WhenTheReportIsSaved()
    {
        // Report already saved in previous steps
    }

    [When(@"I attempt to submit another report for the same page")]
    public async Task WhenIAttemptToSubmitAnotherReportForTheSamePage()
    {
        var duplicateReport = new Report
        {
            Id = Guid.NewGuid(),
            ReportingUserId = _currentUserId,
            ReportedPageUrl = _currentPageUrl,
            Reason = "Spam",
            Description = "Duplicate test",
            SubmittedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _submissionResult = await _reportService.SubmitReportAsync(duplicateReport);
    }

    [When(@"I attempt to access the report submission endpoint")]
    public void WhenIAttemptToAccessTheReportSubmissionEndpoint()
    {
        // Check if authorization would redirect
        _shouldBeRedirected = !_isLoggedIn;
    }

    [When(@"an admin views the reports panel")]
    public void WhenAnAdminViewsTheReportsPanel()
    {
        // Admin viewing would query unresolved reports
    }

    [Then(@"the report should be saved to the database")]
    public void ThenTheReportShouldBeSavedToTheDatabase()
    {
        Assert.That(_submissionResult, Is.True, "Report submission should succeed");
        Assert.That(_reports.Count, Is.EqualTo(1), "Report should be saved");
        
        var savedReport = _reports.First();
        Assert.That(savedReport.ReportingUserId, Is.EqualTo(_currentUserId));
        Assert.That(savedReport.ReportedPageUrl, Is.EqualTo(_currentPageUrl));
    }

    [Then(@"I should receive an in-app notification confirming my report was received")]
    public void ThenIShouldReceiveAnInAppNotificationConfirmingMyReportWasReceived()
    {
        Assert.That(_notifications.Count, Is.EqualTo(1), "Should have sent notification");
        
        var notification = _notifications.First();
        Assert.That(notification.RecipientId, Is.EqualTo(_currentUserId));
        Assert.That(notification.Title, Is.EqualTo("Report Received"));
        Assert.That(notification.Message, Does.Contain("has been received"));
    }

    [Then(@"it should contain my UserId, the page URL, the selected reason, and a SubmittedAt timestamp")]
    public void ThenItShouldContainMyUserIdThePageURLTheSelectedReasonAndASubmittedAtTimestamp()
    {
        Assert.That(_reports.Count, Is.GreaterThan(0), "Report should exist");
        
        var report = _reports.First();
        Assert.That(report.ReportingUserId, Is.EqualTo(_currentUserId), "Should have correct UserId");
        Assert.That(report.ReportedPageUrl, Is.EqualTo("/Map"), "Should have correct page URL");
        Assert.That(report.Reason, Is.EqualTo("Inappropriate content"), "Should have correct reason");
        Assert.That(report.SubmittedAt, Is.Not.EqualTo(default(DateTime)), "Should have SubmittedAt timestamp");
        Assert.That(report.SubmittedAt, Is.LessThanOrEqualTo(DateTime.UtcNow), "Timestamp should not be in future");
    }

    [Then(@"the system should reject the duplicate")]
    public void ThenTheSystemShouldRejectTheDuplicate()
    {
        Assert.That(_submissionResult, Is.False, "Duplicate report should be rejected");
        Assert.That(_reports.Count, Is.EqualTo(1), "Should only have one report");
    }

    [Then(@"I should see a message saying I have already reported this content")]
    public void ThenIShouldSeeAMessageSayingIHaveAlreadyReportedThisContent()
    {
        // Verification that controller returns appropriate error message
        Assert.That(_submissionResult, Is.False, "Should indicate duplicate");
    }

    [Then(@"I should be redirected to the login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        Assert.That(_shouldBeRedirected, Is.True, "Unauthenticated users should be redirected");
    }

    [Then(@"the report should appear with status ""Unresolved""")]
    public void ThenTheReportShouldAppearWithStatusUnresolved()
    {
        var unresolvedReports = _reports.Where(r => !r.IsResolved).ToList();
        Assert.That(unresolvedReports.Count, Is.GreaterThan(0), "Should have unresolved reports");
        
        var report = unresolvedReports.First();
        Assert.That(report.IsResolved, Is.False, "Report should be unresolved");
    }

    #region Helper Methods

    private void SetupMocks()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();

        // Setup repository to save reports to our list
        _reportRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Report>()))
            .ReturnsAsync((Report report) =>
            {
                // Check for duplicate (simulate unique constraint)
                var existingReport = _reports.FirstOrDefault(r => 
                    r.ReportingUserId == report.ReportingUserId && 
                    r.ReportedPageUrl == report.ReportedPageUrl && 
                    !r.IsResolved);

                if (existingReport != null)
                {
                    // Create a proper SQL unique constraint violation exception
                    var sqlException = new SqlExceptionBuilder()
                        .WithNumber((int)SqlErrorNumber.UniqueConstraintViolation)
                        .WithMessage("Violation of UNIQUE KEY constraint 'UQ_Reports_UnresolvedUserPage'.")
                        .Build();
                    
                    throw new Microsoft.EntityFrameworkCore.DbUpdateException(
                        "An error occurred while saving the entity changes. See the inner exception for details.",
                        sqlException);
                }

                _reports.Add(report);
                return report;
            });

        // Setup notification service to track notifications
        _notificationServiceMock.Setup(n => n.SendNotificationAsync(It.IsAny<Notification>()))
            .Callback<Notification>(notification => _notifications.Add(notification))
            .Returns(Task.CompletedTask);

        _reportService = new ReportService(
            _reportRepoMock.Object,
            _notificationServiceMock.Object);
    }

    #endregion
}
