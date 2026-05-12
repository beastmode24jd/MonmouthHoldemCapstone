using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class ReportServiceTests
{
    private Mock<IRepository<Report, ApplicationDbContext>> _reportRepoMock;
    private Mock<INotificationService> _notificationServiceMock;
    private IReportService _reportService;

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();

        _reportService = new ReportService(
            _reportRepoMock.Object,
            _notificationServiceMock.Object
        );
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

    // Pass in different argument for different sorting systems.
    //      Reuse the general code.

    // ReportFilterType values:
            //      - page sort
            //      - reporter sort
            //      - date sort
            //      - resolved sort

            //      Parameter fields are nullable to be omitted as needed.

    [Test]
    public async Task SortReports_ValidReportListAndPageURL_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string pageURL = "/Sighting/456";
        string wrongURL = "/animal/search";

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var reports = new List<Report>
            {
                new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow },
                new Report { ReportedPageUrl = wrongURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow}
            }.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(reports);

        // Act
        var (result, totalCount) = await _reportService.SortReports(
            ReportFilterType.PageURL, 
            pageURL, null, null, false, 1, 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(totalCount, Is.EqualTo(1));

            // Check pageURL values on both report returns.
            Assert.That(result.Count, Is.EqualTo(1)); // Should only find one instance of pageURL
            Assert.That(result[0].ReportedPageUrl, Is.EqualTo(pageURL)); // Should NOT include other URLs.
        });
    }

    [Test]
    public async Task SortReports_ValidReportListAndUserId_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        string pageURL = "/Sighting/456";

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var reports = new List<Report>
        {
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow },
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow.AddHours(-1)},
            new Report { ReportedPageUrl = pageURL, ReportingUserId = wrongUserId, SubmittedAt = DateTime.UtcNow}
        }.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(reports);

        // Act
        var (result, totalCount) = await _reportService.SortReports(
            ReportFilterType.Reporter, 
            null, userId.ToString(), null, false, 1, 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(totalCount, Is.EqualTo(2));

            // Check pageURL values on both report returns.
            Assert.That(result[0].ReportingUserId, Is.EqualTo(userId));
            Assert.That(result[1].ReportingUserId, Is.EqualTo(userId));
            Assert.That(result[0].ReportingUserId, Is.Not.EqualTo(wrongUserId));
            Assert.That(result[1].ReportingUserId, Is.Not.EqualTo(wrongUserId));
        });
    }

    [Test]
    public async Task SortReports_ValidReportListAndTime_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        DateTime olderTime = new DateTime(2000, 1, 1);
        DateTime newerTime = new DateTime(2018, 9, 6);
        string pageURL = "/Sighting/456";

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var reports = new List<Report>
        {
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = olderTime },
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = newerTime}
        }.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(reports);

        // Act
        var (result, totalCount) = await _reportService.SortReports(
            ReportFilterType.Date, null, null, DateTime.UtcNow, false, 1, 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(totalCount, Is.EqualTo(2));

            // Check DateTime values on both report returns.
            Assert.That(result[0].SubmittedAt, Is.EqualTo(newerTime));
            Assert.That(result[1].SubmittedAt, Is.EqualTo(olderTime));
        });
    }

    [Test]
    public async Task SortReports_Pagination_ReturnsCorrectPage()
    {
        // Arrange: Create 5 reports
        var reports = Enumerable.Range(1, 5).Select(i => new Report 
        { 
            ReportedPageUrl = $"/url/{i}", 
            ReportingUserIdentityId = "admin",
            SubmittedAt = DateTime.UtcNow.AddDays(-i) 
        }).AsQueryable().AsAsyncQueryable();

        _reportRepoMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(reports);

        // Act: Request page 2 with a size of 2
        var (result, totalCount) = await _reportService.SortReports(
            ReportFilterType.Date, null, null, null, false, 2, 2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(totalCount, Is.EqualTo(5)); // Total records in DB
            Assert.That(result.Count, Is.EqualTo(2)); // Records on this specific page
        });
    }

    [Test]
    public async Task SortReports_NoReports_ReturnsEmptyList()
    {
        // Arrange
        string pageURL = "/Sighting/456";

        // Set up _reportRepoMock to return no entries (empty list)
        var reports = new List<Report>().AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(reports);
        
        // Act
        var (result, totalCount) = await _reportService.SortReports(
            ReportFilterType.PageURL, 
            pageURL, null, null, false, 1, 10);

        // Assert
        Assert.That(result, Is.Empty, "SortReports should return an empty list if no reports are found.");
    }

    #endregion
}

#region Helper classes

// --- ASYNC QUERYABLE MOCKING HELPERS (used for await _reportRepo.GetAllAsync()) ---

public static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
        => new TestAsyncEnumerable<T>(source);
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default) 
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    // Fix: Explicitly implement the IQueryable.Provider property
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    // This is the logic that ToListAsync() calls
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken token = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethods()
            .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
            .MakeGenericMethod(expectedResultType)
            .Invoke(_inner, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

#endregion