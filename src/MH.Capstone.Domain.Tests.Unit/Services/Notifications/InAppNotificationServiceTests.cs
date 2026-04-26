using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Notifications;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MH.Capstone.Domain.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class InAppNotificationServiceTests : NotificationServiceBaseTests<InAppNotificationService>
    {
        #region TestOverhead

        protected override InAppNotificationService CreateSut()
            => new InAppNotificationService(_mockNotificationRepository.Object,
                _mockUserService.Object,
                NullLogger<INotificationService>.Instance);

        #endregion
        #region SendNotificationAsyncTests

        [TestCase(false)]
        // This should get reset to false when the notification is sent,
        // but we want to make sure that happens regardless of the initial value of IsRead
        [TestCase(true)]
        public async Task SendNotificationAsync_NotificationValid_CreatesNotificationRecordInDbAsUnread(bool readValue)
        {
            // Arrange
            var recipientId = Guid.NewGuid();
            var notif = NotificationValidValuesSource.GetValidNotification(recipientId, isRead: readValue);

            // Expect AddOrUpdateAsync to be called with the notification marked as unread (IsRead == false)
            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.Is<Notification>(n =>
                    n.LinkedUserIdentityId == notif.LinkedUserIdentityId &&
                    n.Title == notif.Title &&
                    n.Message == notif.Message &&
                    n.SentAt == notif.SentAt &&
                    n.IsRead == false)))
                .ReturnsAsync((Notification n) => n)
                .Verifiable();

            // Act
            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded);

            // Assert
            AssertAllMockVerifications();
        }

        [Test]
        public void SendNotificationAsync_RecipientIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var notif = NotificationValidValuesSource.GetValidNotification();

            // Simulate SQL foreign key violation when attempting to persist - repository throws DbUpdateException with inner SqlException(547)
            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(
                    It.Is<Notification>(n => n.Id == notif.Id)))
                .ThrowsAsync(new DbUpdateException("Foreign key violation",
                    new SqlExceptionBuilder().WithNumber(547).Build()))
                .Verifiable();

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded));
            AssertAllMockVerifications();
        }

        [TestCase(-1)] // Means null
        [TestCase(0)] // Means empty string
        [TestCase(52)] // Exceeds max length of 50
        public void SendNotificationAsync_InvalidTitle_ReturnsFailedTaskThrowingArgumentException(int titleLen)
        {
            // Arrange
            var notif = NotificationValidValuesSource.GetValidNotification();

            if (titleLen == -1)
                notif.Title = null!;
            else if (titleLen == 0)
                notif.Title = string.Empty;
            else
                notif.Title = RandomData.GetRandomStringOfLength(titleLen);

            var sut = CreateSut();

            // Act & Assert - should fail validation before repository is called
            Assert.ThrowsAsync<ArgumentException>(() => sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded));
            AssertAllMockVerifications();
        }

        [TestCase(-1)] // Means null
        [TestCase(0)] // Means empty string
        [TestCase(257)] // Exceeds max length of 250
        public void SendNotificationAsync_InvalidMessage_ReturnsFailedTaskThrowingArgumentException(int messageLen)
        {
            // Arrange
            var notif = NotificationValidValuesSource.GetValidNotification();

            if (messageLen == -1)
                notif.Message = null!;
            else if (messageLen == 0)
                notif.Message = string.Empty;
            else
                notif.Message = RandomData.GetRandomStringOfLength(messageLen);

            var sut = CreateSut();

            // Act & Assert - should fail validation before repository is called
            Assert.ThrowsAsync<ArgumentException>(() => sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded));
            AssertAllMockVerifications();
        }

        #endregion
    }
}
