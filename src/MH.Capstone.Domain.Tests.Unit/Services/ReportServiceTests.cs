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
    private string _adminId;  // Used for Report filtering checks
    private Guid value; // Creates GUID for _adminId conversion

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();
        value = new Guid();
        _adminId = value.ToString();
    }

    public ReportService CreateSut() => new(
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
                It.Is<Notification>(notif => notif.RecipientId == userId), It.IsAny<NotificationType>()))
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
                It.Is<Notification>(notif => notif.RecipientId == userId), It.IsAny<NotificationType>()))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        await sut.SubmitReportAsync(report);

        // Assert
        AssertAllMockVerifications();
    }

    #region Report Filter
    // [CSP-179] ***************************

    // Need methods to filter based on page,
    //      reporter (associated ApplicationUser),
    //      and date.

    //      Maybe require Admin Id as a guard check?

    // Pass in different argument for different sorting systems.
    //      Reuse the general code.

    // Int types:
            //      0 == page sort
            //      1 == reporter sort
            //      2 == date sort
            //      Parameter fields are nullable to be omitted as needed.

    [Test]
    public async Task SortReports_ValidReportListAndPageURL_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string pageURL = "/Sighting/456";
        string wrongURL = "/animal/search";

        var reportOne = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageURL,
            Reason = "Inaccurate information",
            SubmittedAt = DateTime.UtcNow
        };
        var reportTwo = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = wrongURL,
            Reason = "Page error",
            SubmittedAt = DateTime.UtcNow
        };

        var reportList = new List<Report>{reportOne, reportTwo};

        // Save _adminId value to context

        // Save reportList to _reportRepoMock

        //_reportRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Report>()))
            //.ReturnsAsync(reportList)
            //.Verifiable(Times.Once);

        // Act
        // var result = await _reportService.SortReports(_adminId, 0, pageURL, null, null);

        /*
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));

            // Check pageURL values on both report returns.
            Assert.That(result[0].ReportedPageUrl, Is.EqualTo(pageURL)); // Should include searched for entry
            Assert.That(result[0].ReportedPageUrl, Is.Not.EqualTo(wrongURL)); // Should NOT include other URLs.
        });
        */
    }

    [Test]
    public async Task SortReports_ValidReportListAndUserId_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        string pageURL = "/Sighting/456";

        var reportOne = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageURL,
            Reason = "Inaccurate information",
            SubmittedAt = DateTime.UtcNow
        };
        var reportTwo = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageURL,
            Reason = "Page error",
            SubmittedAt = DateTime.UtcNow
        };
        var reportThree = new Report
        {
            ReportingUserId = wrongUserId,
            ReportedPageUrl = pageURL,
            Reason = "I don't like the buttons",
            SubmittedAt = DateTime.UtcNow
        };

        var reportList = new List<Report>{reportOne, reportTwo, reportThree};

        /*
        // Act
        var result = await _reportService.SortReports(_adminId, 1, null, userId, null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

            // Check pageURL values on both report returns.
            Assert.That(result[0].ReportingUserId, Is.EqualTo(userId));
            Assert.That(result[1].ReportingUserId, Is.EqualTo(userId));
            Assert.That(result[0].ReportingUserId, Is.Not.EqualTo(wrongUserId));
            Assert.That(result[1].ReportingUserId, Is.Not.EqualTo(wrongUserId));
        }); */
    }

    [Test]
    public async Task SortReports_ValidReportListAndTime_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        DateTime olderTime = new DateTime(2000, 1, 1);
        DateTime newerTime = new DateTime(2018, 9, 6);
        string pageURL = "/Sighting/456";

        var reportOne = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageURL,
            Reason = "Inaccurate information",
            SubmittedAt = olderTime
        };
        var reportTwo = new Report
        {
            ReportingUserId = userId,
            ReportedPageUrl = pageURL,
            Reason = "Page error",
            SubmittedAt = newerTime
        };

        var reportList = new List<Report>{reportOne, reportTwo};

        // Save adminId to mockUserRepo for Id check,
        //  Save reportList to mockReportRepo

        /*
        // Act
        var result = await _reportService.SortReports(_adminId, 2, null, null, DateTime.UtcNow);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

            // Check DateTime values on both report returns.
            Assert.That(result[0].SubmittedAt, Is.EqualTo(newerTime));
            Assert.That(result[1].SubmittedAt, Is.EqualTo(olderTime));
        }); */
    }

    // Use [TestCase(values)] for multiple tests here?
    [Test]
    public async Task SortReports_InvalidReportListParams_ReturnsException()
    {
        
    }

    [Test]
    public async Task SortReports_NoReports_ReturnsEmptyList()
    {
        // Arrange
        string pageURL = "/Sighting/456";

        // Set up _reportRepoMock to return no entries.

        // Set up adminId for admin lookup.

        /*
        // Act
        var result = await _reportService.SortReports(_adminId, 0, pageURL, null, null);

        // Assert
        Assert.That(result, Is.Empty, "SortReports should return an empty list if no reports are found.");
        */
    }

    #endregion
}