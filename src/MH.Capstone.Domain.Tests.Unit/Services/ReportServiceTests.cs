using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.EntityFrameworkCore;
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

        var duplicateReport = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageUrl,
            Reason = "Inappropriate content"
        };

        // Database will throw unique constraint violation on duplicate
        var uniqueConstraintException = new SqlExceptionBuilder()
            .WithNumber((int)SqlErrorNumber.UniqueConstraintViolation)
            .Build();
        var dbUpdateException = new DbUpdateException("Duplicate key", uniqueConstraintException);

        _reportRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Report>()))
            .ThrowsAsync(dbUpdateException)
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