using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Notifications;
using static MH.Capstone.Tests.SharedInternals.RandomData;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Tests.SharedInternals;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public abstract class NotificationServiceBaseTests<TService> where TService : NotificationServiceBase
    {
        #region TestOverhead

        protected Mock<IRepository<Notification, ApplicationDbContext>> _mockNotificationRepository;

        [SetUp]
        public void SetUp()
        {
            _mockNotificationRepository =
                new Mock<IRepository<Notification, ApplicationDbContext>>();
        }

        protected abstract TService CreateSut();

        protected void AssertAllMockVerifications()
        {
            // Asserts that the methods that were set up in the Moq were called in ways that we set up
            _mockNotificationRepository.VerifyAll();

            // Asserts that the Moq mocks were only called in ways that we set up with the Setup method,
            // failing if any method was called that was not set up
            _mockNotificationRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetPendingNotificationsAsyncTests

        [Test]
        public async Task GetPendingNotificationsAsync_HasNoPendingNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(userId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task
            GetPendingNotificationsAsync_HasPendingNotifications_ReturnsTaskWithEnumerableOfPendingNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var pending = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userId),
                NotificationValidValuesSource.GetValidNotification(userId)
            };

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ReturnsAsync(pending.AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(userId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(pending.Length));
            Assert.That(
                result.Select(n => n.Id),
                Is.EquivalentTo(pending.Select(p => p.Id))
            );
            AssertAllMockVerifications();
        }

        [Test]
        public void GetPendingNotificationsAsync_UserIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ThrowsAsync(new ArgumentException("User id not found"))
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetPendingNotificationsAsync(userId));
            AssertAllMockVerifications();
        }

        #endregion

        #region GetAllNotificationsAsyncTests

        [Test]
        public async Task GetAllNotificationsAsync_HasNoNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Some implementations may call the overload with a predicate; others the parameterless overload.
            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.AtMostOnce);

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.AtMostOnce);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(userId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            AssertAllMockVerifications();
        }

        [Test]
        public async Task GetPendingNotificationsAsync_HasNotifications_ReturnsTaskWithEnumerableOfNotifications()
        {
            // Note: Name kept as provided in the test skeleton. This test targets GetAllNotificationsAsync scenario.
            // Arrange
            var userId = Guid.NewGuid();
            var all = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userId),
                NotificationValidValuesSource.GetValidNotification(userId, true)
            };

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ReturnsAsync(all.AsQueryable())
                .Verifiable();

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(userId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(all.Length));
            Assert.That(
                result.Select(n => n.Id),
                Is.EquivalentTo(all.Select(a => a.Id))
            );
            AssertAllMockVerifications();
        }

        [Test]
        public void GetAllNotificationsAsync_UserIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                .ThrowsAsync(new ArgumentException("User id not found"))
                .Verifiable();

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetAllNotificationsAsync(userId));
            AssertAllMockVerifications();
        }

        #endregion

        #region MarkNotificationAsReadAsyncTests

        [Test]
        public void MarkNotificationAsReadAsync_NotificationIdNotFound_ReturnsFailedResultThrowingArgumentException()
        {
            // Arrange
            var notifId = Guid.NewGuid();

            _mockNotificationRepository
                .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Notification?)null)
                .Verifiable(Times.Once);

            // Some implementations of repositories may expose FindByIdAsync(int) only; however,
            // tests in this project commonly use FindByIdAsync for lookup. If the concrete SUT uses
            // a different repository method, the concrete test CreateSut implementation should map
            // accordingly. For our purposes we verify that a lookup that yields null results in an ArgumentException.

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.MarkNotificationAsReadAsync(notifId));
            AssertAllMockVerifications();
        }

        [Test]
        public async Task MarkNotificationAsReadAsync_NotificationFound_MarksNotificationAsRead()
        {
            // Arrange
            var notifId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notifId,
                RecipientIdentityId = Guid.NewGuid().ToString(),
                Title = "Test",
                Message = "Message",
                SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                IsRead = false
            };

            // When the service looks up the notification it should receive the entity
            _mockNotificationRepository
                .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(notification)
                .Verifiable(Times.Once);

            // Expect AddOrUpdateAsync to be called with the notification marked as read
            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.Is<Notification>(n => n.Id == notifId && n.IsRead)))
                .ReturnsAsync((Notification n) => n)
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            await sut.MarkNotificationAsReadAsync(notifId);

            // Assert
            AssertAllMockVerifications();
        }

        #endregion
    }

    public struct NotificationValidValuesSource
    {
        public static Notification GetValidNotification(Guid? recipientId = null, bool isRead = false) =>
            new Notification(Guid.NewGuid(), recipientId ?? Guid.NewGuid(),
                GetRandomStringOfLength(GetRandomIntInRange(1, 50)),
                GetRandomStringOfLength(GetRandomIntInRange(1, 250)),
                DateTimeOffset.UtcNow)
            {
                IsRead = isRead
            };
    }
}