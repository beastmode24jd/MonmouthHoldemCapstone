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
public class AuditServiceTests
{
    private Mock<IRepository<Report, ApplicationDbContext>> _reportRepoMock;
    private Mock<IRepository<AuditLog, ApplicationDbContext>> _auditRepoMock;
    private IAuditService _auditService;

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _auditRepoMock = new Mock<IRepository<AuditLog, ApplicationDbContext>>();

        _auditService = new AuditService(
            _reportRepoMock.Object,
            _auditRepoMock.Object
        );
    }

    public AuditService CreateSut() => new(
        _reportRepoMock.Object,
        _auditRepoMock.Object);

    private void AssertAllMockVerifications()
    {
        _reportRepoMock.VerifyAll();
        _auditRepoMock.VerifyAll();
        _reportRepoMock.VerifyNoOtherCalls();
        _auditRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetAuditsByActionAsync_FiltersCorrectlyAndReturnsCount()
    {
        // Arrange
        var actionToFind = AuditActionType.UserLocked;
        var otherAction = AuditActionType.ReportResolved;

        var data = new List<AuditLog>
        {
            new AuditLog { ActionType = actionToFind, Timestamp = DateTimeOffset.UtcNow },
            new AuditLog { ActionType = actionToFind, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new AuditLog { ActionType = otherAction, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2) }
        }.AsQueryable();

        // Mock the repository to return our list
        _auditRepoMock.Setup(r => r.GetAllAsync()).Returns(data);

        var sut = CreateSut();

        // Act
        var (results, count) = await sut.GetAuditsByActionAsync(actionToFind, 1, 10);

        // Assert
        results.Should().HaveCount(2);
        results.All(a => a.ActionType == actionToFind).Should().BeTrue();
        count.Should().Be(2);
    }

    [Test]
    public async Task GetPagedAuditsAsync_AppliesPaginationCorrectly()
    {
        // Arrange: Create 15 items
        var data = Enumerable.Range(1, 15).Select(i => new AuditLog { 
            Id = Guid.NewGuid(), 
            Timestamp = DateTimeOffset.UtcNow.AddDays(-i) 
        }).AsQueryable();

        _auditRepoMock.Setup(r => r.GetAllAsync()).Returns(data);
        var sut = CreateSut();

        // Act: Get Page 2 with size 10
        var (results, count) = await sut.GetPagedAuditsAsync(2, 10);

        // Assert
        results.Should().HaveCount(5); // 15 total - 10 on first page = 5 left
        count.Should().Be(15);
    }
}