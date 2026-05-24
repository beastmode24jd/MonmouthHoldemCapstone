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
    private Mock<INotificationService> _notificationServiceMock;
    private IAuditService _auditService;

    [SetUp]
    public void Setup()
    {
        _reportRepoMock = new Mock<IRepository<Report, ApplicationDbContext>>();
        _auditRepoMock = new Mock<IRepository<AuditLog, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();

        _auditService = new AuditService(
            _reportRepoMock.Object,
            _notificationServiceMock.Object
        );
    }

    public AuditService CreateSut() => new(
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
    public async Task SubmitAuditAsync_NewLog_ReturnsTrueAndSavesAudit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var audit = new AuditLog
        {
            TargetUserId = userId,
            PerformingUserId = adminId,
            ActionType = AuditActionType.ReportResolved,
            Details = "Duplicate page report",
            Timestamp = DateTimeOffset.UtcNow
        };

        _auditRepoMock.Setup(r => r.AddOrUpdateAsync(
                It.Is<AuditLog>(aud => aud.TargetUserId == userId && aud.PerformingUserId == adminId)))
            .ReturnsAsync(audit)
            .Verifiable(Times.Once);

        /*
        _notificationServiceMock.Setup(n => n.SendNotificationAsync(
                It.Is<Notification>(notif => notif.RecipientId == userId), It.IsAny<NotificationType>()))
            .Verifiable(Times.Once); */

        var sut = CreateSut();

        // Act
        //var result = await sut.SubmitReportAsync(report);
        var result = false;

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifications();
    }
}