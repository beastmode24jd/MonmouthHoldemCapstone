using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class ReportServiceTests
{
    private Mock<IRepository<Report, ApplicationDbContext>> _reportRepoMock;
    private Mock<INotificationService> _notificationServiceMock;

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();
    }

    private ReportService CreateSut() => new(
        _reportRepoMock.Object,
        _notificationServiceMock.Object);

    private void AssertAllMockVerifications()
    {
        _reportRepoMock.VerifyAll();
        _notificationServiceMock.VerifyAll();
        _reportRepoMock.VerifyNoOtherCalls();
        _notificationServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SubmitReportAsync_NewReport_ReturnsTrueAndSavesReport()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = "/Sighting/123",
            Reason = "Inappropriate content",
            SubmittedAt = DateTime.UtcNow
        };

        // No existing reports for this user + URL
        _reportRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Report>().AsQueryable())
            .Verifiable(Times.Once);

        _reportRepoMock.Setup(r => r.AddOrUpdateAsync(
                It.Is<Report>(rep => rep.ReportingUserId == userId && rep.ReportedPageUrl == report.ReportedPageUrl)))
            .ReturnsAsync(report)
            .Verifiable(Times.Once);

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(
                It.Is<Notification>(notif => notif.RecipientId == userId)))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitReportAsync(report);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifications();
    }

    [Test]
    public async Task SubmitReportAsync_DuplicateReport_ReturnsFalseAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pageUrl = "/Sighting/123";

        var existingReport = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageUrl,
            Reason = "Spam",
            SubmittedAt = DateTime.UtcNow
        };

        var duplicateReport = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageUrl,
            Reason = "Inappropriate content"
        };

        // A report already exists for this user + URL
        _reportRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Report> { existingReport }.AsQueryable())
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitReportAsync(duplicateReport);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifications(); 
    }

    [Test]
    public async Task SubmitReportAsync_SuccessfulSubmission_SendsConfirmationNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = "/Sighting/456",
            Reason = "Inaccurate information",
            SubmittedAt = DateTime.UtcNow
        };

        _reportRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Report>().AsQueryable())
            .Verifiable(Times.Once);

        _reportRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Report>()))
            .ReturnsAsync(report)
            .Verifiable(Times.Once);

        // notification must go to the reporting user
        _notificationServiceMock.Setup(n => n.SendNotificationAsync(
                It.Is<Notification>(notif => notif.RecipientId == userId)))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        await sut.SubmitReportAsync(report);

        // Assert
        AssertAllMockVerifications();
    }
}