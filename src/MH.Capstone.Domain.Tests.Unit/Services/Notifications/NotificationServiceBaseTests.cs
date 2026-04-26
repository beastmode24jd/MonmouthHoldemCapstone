using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Neleus.LambdaCompare;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using MH.Capstone.Tests.SharedInternals;
using static MH.Capstone.Tests.SharedInternals.RandomData;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public abstract class NotificationServiceBaseTests<TService> where TService : NotificationServiceBase
    {
        #region TestOverhead

        protected Mock<IRepository<Notification, ApplicationDbContext>> _mockNotificationRepository;
        protected Mock<IUserService> _mockUserService;

        [SetUp]
        public void SetUp()
        {
            _mockNotificationRepository =
                new Mock<IRepository<Notification, ApplicationDbContext>>();
            _mockUserService = new Mock<IUserService>();
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

        private static Expression<Func<Notification, bool>> CreatePendingNotificationTestExpression(Guid userId)
            => n => n.LinkedUserIdentityId == userId.ToString() && !n.IsRead;

        [Test]
        public async Task GetPendingNotificationsAsyncGuidId_HasNoPendingNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreatePendingNotificationTestExpression(userToAssert.GuidId);

            _mockUserService.Setup(s => 
                s.GetUserByIdAsync(It.Is<Guid>(id => id == userToAssert.GuidId)))
                .ReturnsAsync(userToAssert).Verifiable(Times.Once);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(userToAssert.GuidId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPendingNotificationsAsyncUserModel_HasNoPendingNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreatePendingNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(user)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task
            GetPendingNotificationsAsyncGuidId_HasPendingNotifications_ReturnsTaskWithEnumerableOfPendingNotifications()
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var pending = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userToAssert.GuidId),
                NotificationValidValuesSource.GetValidNotification(userToAssert.GuidId)
            };
            var testExpression = CreatePendingNotificationTestExpression(userToAssert.GuidId);

            _mockUserService.Setup(s =>
                    s.GetUserByIdAsync(It.Is<Guid>(id => id == userToAssert.GuidId)))
                .ReturnsAsync(userToAssert).Verifiable(Times.Once);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(pending.AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(userToAssert.GuidId)).ToList();

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
        public async Task
            GetPendingNotificationsAsyncUserModel_HasPendingNotifications_ReturnsTaskWithEnumerableOfPendingNotifications()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var pending = new[]
            {
                NotificationValidValuesSource.GetValidNotification(user.GuidId),
                NotificationValidValuesSource.GetValidNotification(user.GuidId)
            };
            var testExpression = CreatePendingNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(pending.AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetPendingNotificationsAsync(user)).ToList();

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
        public void GetPendingNotificationsAsyncGuidId_UserIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserService.Setup(s => 
                s.UserExistsAsync(userId.ToString()))
                .ReturnsAsync(false).Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetPendingNotificationsAsync(userId));
            AssertAllMockVerifications();
        }

        [Test]
        public void GetPendingNotificationsAsyncUserModel_UserNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreatePendingNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                        ))
                .ThrowsAsync(new SqlExceptionBuilder().WithNumber(
                        (int)SqlErrorNumber.ForeignKeyConstraintViolation).Build())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetPendingNotificationsAsync(user));
            AssertAllMockVerifications();
        }

        #endregion

        #region GetAllNotificationsAsyncTests

        private static Expression<Func<Notification, bool>> CreateAllNotificationTestExpression(Guid userId)
            => n => n.LinkedUserIdentityId == userId.ToString();

        [Test]
        public async Task GetAllNotificationsAsyncGuidId_HasNoNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreateAllNotificationTestExpression(userToAssert.GuidId);

            _mockUserService.Setup(s => 
                s.GetUserByIdAsync(It.Is<Guid>(id => id == userToAssert.GuidId)))
                .ReturnsAsync(userToAssert).Verifiable(Times.Once);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(userToAssert.GuidId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            AssertAllMockVerifications();
        }

        [Test]
        public async Task GetAllNotificationsAsyncUserModel_HasNoNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreateAllNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(user)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            AssertAllMockVerifications();
        }

        [Test]
        public async Task GetPendingNotificationsAsyncGuidId_HasNotifications_ReturnsTaskWithEnumerableOfNotifications()
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var all = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userToAssert.GuidId),
                NotificationValidValuesSource.GetValidNotification(userToAssert.GuidId, true)
            };
            var testExpression = CreateAllNotificationTestExpression(userToAssert.GuidId);

            _mockUserService.Setup(s =>
                    s.GetUserByIdAsync(It.Is<Guid>(id => id == userToAssert.GuidId)))
                .ReturnsAsync(userToAssert).Verifiable(Times.Once);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(all.AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(userToAssert.GuidId)).ToList();

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
        public async Task GetPendingNotificationsAsyncUserModel_HasNotifications_ReturnsTaskWithEnumerableOfNotifications()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var all = new[]
            {
                NotificationValidValuesSource.GetValidNotification(user.GuidId),
                NotificationValidValuesSource.GetValidNotification(user.GuidId, true)
            };
            var testExpression = CreateAllNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(all.AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(user)).ToList();

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
        public void GetAllNotificationsAsyncGuidId_UserIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _mockUserService.Setup(s => 
                    s.UserExistsAsync(userId.ToString()))
                .ReturnsAsync(false).Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetAllNotificationsAsync(userId));
            AssertAllMockVerifications();
        }

        [Test]
        public void GetAllNotificationsAsyncUserModel_UserIdNotFound_ReturnsFailedTaskThrowingArgumentException()
        {
            // Arrange
            var user = new ApplicationUser
            {
                GuidId = Guid.NewGuid()
            };
            var testExpression = CreateAllNotificationTestExpression(user.GuidId);

            _mockNotificationRepository.Setup(r =>
                r.GetAllAsync(
                    It.Is<Expression<Func<Notification, bool>>>(
                        e => Lambda.ExpressionsEqual(e, testExpression))
                ))
                .ThrowsAsync(new SqlExceptionBuilder().WithNumber(
                    (int)SqlErrorNumber.ForeignKeyConstraintViolation).Build())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetAllNotificationsAsync(user));
            AssertAllMockVerifications();
        }

        #endregion

        #region MarkAllAsReadAsyncTests

        private static Expression<Func<Notification, bool>> CreateUnreadNotificationTestExpression(Guid userId)
            => n => n.LinkedUserIdentityId == userId.ToString() && !n.IsRead;

        [Test]
        public async Task MarkAllAsReadAsync_HasUnreadNotifications_MarksAllAsRead()
        {
            // Arrange
            var user = new ApplicationUser { GuidId = Guid.NewGuid() };
            var unreadNotifs = new[]
            {
                NotificationValidValuesSource.GetValidNotification(user.GuidId, isRead: false),
                NotificationValidValuesSource.GetValidNotification(user.GuidId, isRead: false)
            };
            var testExpression = CreateUnreadNotificationTestExpression(user.GuidId);

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.Is<Expression<Func<Notification, bool>>>(
                    e => Lambda.ExpressionsEqual(e, testExpression))))
                .ReturnsAsync(unreadNotifs.AsQueryable())
                .Verifiable(Times.Once);

            foreach (var notif in unreadNotifs)
            {
                var capturedId = notif.Id;
                _mockNotificationRepository
                    .Setup(r => r.AddOrUpdateAsync(It.Is<Notification>(n => n.Id == capturedId && n.IsRead)))
                    .ReturnsAsync(notif)
                    .Verifiable(Times.Once);
            }

            // Act
            var sut = CreateSut();
            await sut.MarkAllAsReadAsync(user);

            // Assert
            Assert.That(unreadNotifs.All(n => n.IsRead), Is.True);
            _mockNotificationRepository.VerifyAll();
            _mockNotificationRepository.VerifyNoOtherCalls();
        }

        [Test]
        public async Task MarkAllAsReadAsync_HasNoUnreadNotifications_DoesNotCallAddOrUpdate()
        {
            // Arrange
            var user = new ApplicationUser { GuidId = Guid.NewGuid() };
            var testExpression = CreateUnreadNotificationTestExpression(user.GuidId);

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.Is<Expression<Func<Notification, bool>>>(
                    e => Lambda.ExpressionsEqual(e, testExpression))))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            await sut.MarkAllAsReadAsync(user);

            // Assert — AddOrUpdateAsync should never be called when there are no unread notifications
            _mockNotificationRepository.VerifyAll();
            _mockNotificationRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region DeleteAllAsyncTests

        [Test]
        public async Task DeleteAllAsync_HasNotifications_DeletesAll()
        {
            // Arrange
            var user = new ApplicationUser { GuidId = Guid.NewGuid() };
            var notifications = new[]
            {
                NotificationValidValuesSource.GetValidNotification(user.GuidId, isRead: false),
                NotificationValidValuesSource.GetValidNotification(user.GuidId, isRead: true)
            };
            var testExpression = CreateAllNotificationTestExpression(user.GuidId);

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.Is<Expression<Func<Notification, bool>>>(
                    e => Lambda.ExpressionsEqual(e, testExpression))))
                .ReturnsAsync(notifications.AsQueryable())
                .Verifiable(Times.Once);

            foreach (var notif in notifications)
            {
                var capturedId = notif.Id;
                _mockNotificationRepository
                    .Setup(r => r.DeleteAsync(It.Is<Notification>(n => n.Id == capturedId)))
                    .Returns(Task.CompletedTask)
                    .Verifiable(Times.Once);
            }

            // Act
            var sut = CreateSut();
            await sut.DeleteAllAsync(user);

            // Assert
            _mockNotificationRepository.VerifyAll();
            _mockNotificationRepository.VerifyNoOtherCalls();
        }

        [Test]
        public async Task DeleteAllAsync_HasNoNotifications_DoesNotCallDelete()
        {
            // Arrange
            var user = new ApplicationUser { GuidId = Guid.NewGuid() };
            var testExpression = CreateAllNotificationTestExpression(user.GuidId);

            _mockNotificationRepository
                .Setup(r => r.GetAllAsync(It.Is<Expression<Func<Notification, bool>>>(
                    e => Lambda.ExpressionsEqual(e, testExpression))))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            await sut.DeleteAllAsync(user);

            // Assert — DeleteAsync should never be called when there are no notifications
            _mockNotificationRepository.VerifyAll();
            _mockNotificationRepository.VerifyNoOtherCalls();
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