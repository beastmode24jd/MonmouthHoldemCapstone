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
            => n => n.RecipientId == userId && !n.IsRead;

        [Test]
        public async Task GetPendingNotificationsAsyncGuidId_HasNoPendingNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testExpression = CreatePendingNotificationTestExpression(userId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
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
            var userId = Guid.NewGuid();
            var pending = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userId),
                NotificationValidValuesSource.GetValidNotification(userId)
            };
            var testExpression = CreatePendingNotificationTestExpression(userId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
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
                .ThrowsAsync(new ArgumentException("User id not found"))
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
            => n => n.RecipientId == userId;

        [Test]
        public async Task GetAllNotificationsAsyncGuidId_HasNoNotifications_ReturnsTaskWithEmptyEnumerable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testExpression = CreateAllNotificationTestExpression(userId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(Enumerable.Empty<Notification>().AsQueryable())
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();
            var result = (await sut.GetAllNotificationsAsync(userId)).ToList();

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
            var userId = Guid.NewGuid();
            var all = new[]
            {
                NotificationValidValuesSource.GetValidNotification(userId),
                NotificationValidValuesSource.GetValidNotification(userId, true)
            };
            var testExpression = CreateAllNotificationTestExpression(userId);

            _mockNotificationRepository.Setup(r =>
                    r.GetAllAsync(
                        It.Is<Expression<Func<Notification, bool>>>(
                            e => Lambda.ExpressionsEqual(e, testExpression))
                    ))
                .ReturnsAsync(all.AsQueryable())
                .Verifiable(Times.Once);

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
                .ThrowsAsync(new ArgumentException("User id not found"))
                .Verifiable(Times.Once);

            // Act
            var sut = CreateSut();

            // Assert
            Assert.ThrowsAsync<ArgumentException>(() => sut.GetAllNotificationsAsync(user));
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