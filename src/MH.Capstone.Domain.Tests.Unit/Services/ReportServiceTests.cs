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
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;
    private string _adminId;  // Used for Report filtering checks
    private Guid _adminIdGUID; // Creates GUID for _adminId conversion
    private ApplicationUser _adminUser;

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();

        // This is only used for the UserManager Mock.
        // Only called internally by the UserService, don't need to worry about verifying or
        // setting up any of it's methods.
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();

        // Pass in nulls for all the other dependencies of UserManager
        // Mock the method outputs of UserManager
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _adminIdGUID = Guid.NewGuid();
        _adminId = _adminIdGUID.ToString();

        // Create the admin account for Id checks
        _adminUser = new ApplicationUser();
        _adminUser.Id = _adminId;
        _adminUser.UserName = "admin@test.com";

        // Setup AdminId Check Mocks
        _userManagerMock.Setup(u => u.FindByIdAsync(_adminId)).ReturnsAsync(_adminUser);
        _userManagerMock.Setup(u => u.IsInRoleAsync(_adminUser, "Admin")).ReturnsAsync(true);

        _reportService = new ReportService(
            _reportRepoMock.Object,
            _notificationServiceMock.Object,
            _userManagerMock.Object,
            _userRepoMock.Object
        );
    }

    public ReportService CreateSut() => new(
        _reportRepoMock.Object,
        _notificationServiceMock.Object,
        _userManagerMock.Object,
        _userRepoMock.Object);

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
            //      Parameter fields are nullable to be omitted as needed.

    [Test]
    public async Task SortReports_ValidReportListAndPageURL_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string pageURL = "/Sighting/456";
        string wrongURL = "/animal/search";

        var reports = new List<Report>
            {
                new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow },
                new Report { ReportedPageUrl = wrongURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow}
            };

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var mockAsyncQueryable = reports.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(mockAsyncQueryable);

        // Act
        var result = await _reportService.SortReports(
            _adminId, ReportFilterType.PageURL, 
            pageURL, null, null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));

            // Check pageURL values on both report returns.
            Assert.That(result[0].ReportedPageUrl, Is.EqualTo(pageURL)); // Should include searched for entry
            Assert.That(result[0].ReportedPageUrl, Is.Not.EqualTo(wrongURL)); // Should NOT include other URLs.
        });
    }

    [Test]
    public async Task SortReports_ValidReportListAndUserId_ReturnsSortedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        string pageURL = "/Sighting/456";

        var reports = new List<Report>
        {
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow },
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow.AddHours(-1)},
            new Report { ReportedPageUrl = pageURL, ReportingUserId = wrongUserId, SubmittedAt = DateTime.UtcNow}
        };

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var mockAsyncQueryable = reports.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(mockAsyncQueryable);

        // Act
        var result = await _reportService.SortReports(
            _adminId, ReportFilterType.Reporter, 
            null, userId.ToString(), null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

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

        var reports = new List<Report>
        {
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = olderTime },
            new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = newerTime}
        };

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var mockAsyncQueryable = reports.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(mockAsyncQueryable);

        // Act
        var result = await _reportService.SortReports(
            _adminId, ReportFilterType.Date, 
            null, null, DateTime.UtcNow);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

            // Check DateTime values on both report returns.
            Assert.That(result[0].SubmittedAt, Is.EqualTo(newerTime));
            Assert.That(result[1].SubmittedAt, Is.EqualTo(olderTime));
        });
    }

    // Use [TestCase(values)] for multiple tests here?
    [Test]
    public async Task SortReports_InvalidReportListParams_ReturnsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string pageURL = "/Sighting/456";

        var reports = new List<Report>
            {
                new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow },
                new Report { ReportedPageUrl = pageURL, ReportingUserId = userId, SubmittedAt = DateTime.UtcNow}
            };

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var mockAsyncQueryable = reports.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(mockAsyncQueryable);

        // Act and Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(async() => await _reportService.SortReports(
            userId.ToString(), ReportFilterType.PageURL, 
            pageURL, null, null));
    }

    [Test]
    public async Task SortReports_NoReports_ReturnsEmptyList()
    {
        // Arrange
        string pageURL = "/Sighting/456";

        // Set up _reportRepoMock to return no entries (empty list)
        var reports = new List<Report>();

        // Convert list into a Mockable Async IQueryable for _reportRepoMock
        var mockAsyncQueryable = reports.AsQueryable().AsAsyncQueryable();

        // Save reportList to _reportRepoMock
        _reportRepoMock.Setup(repo => repo.GetAllAsync())
                           .ReturnsAsync(mockAsyncQueryable);
        
        // Act
        var result = await _reportService.SortReports(
            _adminId, ReportFilterType.PageURL, 
            pageURL, null, null);

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