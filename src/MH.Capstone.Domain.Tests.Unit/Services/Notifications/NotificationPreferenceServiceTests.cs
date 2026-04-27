using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Notifications;
using Moq;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class NotificationPreferenceServiceTests
    {
        private Mock<IRepository<UserNotificationPreference, ApplicationDbContext>> _mockPreferenceRepo;
        private ApplicationUser _user;

        [SetUp]
        public void SetUp()
        {
            _mockPreferenceRepo = new Mock<IRepository<UserNotificationPreference, ApplicationDbContext>>();
            _user = new ApplicationUser { Id = Guid.NewGuid().ToString() };
        }

        private NotificationPreferenceService CreateSut() => new NotificationPreferenceService(_mockPreferenceRepo.Object);

        #region GetPreferencesAsync

        [Test]
        public async Task GetPreferencesAsync_ReturnsOnlyConfigurableTypes_ExcludesSystemCritical()
        {
            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<UserNotificationPreference>().AsQueryable());

            var sut = CreateSut();
            var result = (await sut.GetPreferencesAsync(_user)).ToList();

            Assert.That(result.All(p => p.NotificationType != NotificationType.SystemCritical), Is.True);
        }

        [Test]
        public async Task GetPreferencesAsync_UserHasStoredPreferences_ReturnsStoredValues()
        {
            var stored = new List<UserNotificationPreference>
            {
                new UserNotificationPreference(_user.Id, NotificationType.BadgeAwarded, NotificationDeliveryChannel.EmailOnly),
                new UserNotificationPreference(_user.Id, NotificationType.ReportStatusUpdate, NotificationDeliveryChannel.Silenced)
            };

            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(stored.AsQueryable());

            var sut = CreateSut();
            var result = (await sut.GetPreferencesAsync(_user)).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(p => p.NotificationType == NotificationType.BadgeAwarded && p.DeliveryChannel == NotificationDeliveryChannel.EmailOnly), Is.True);
            Assert.That(result.Any(p => p.NotificationType == NotificationType.ReportStatusUpdate && p.DeliveryChannel == NotificationDeliveryChannel.Silenced), Is.True);
        }

        #endregion

        #region GetDeliveryChannelAsync

        [Test]
        public async Task GetDeliveryChannelAsync_SystemCritical_AlwaysReturnsInAppAndEmail()
        {
            var sut = CreateSut();
            var result = await sut.GetDeliveryChannelAsync(_user, NotificationType.SystemCritical);
            Assert.That(result, Is.EqualTo(NotificationDeliveryChannel.InAppAndEmail));
            _mockPreferenceRepo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetDeliveryChannelAsync_StoredPreferenceExists_ReturnsStoredChannel()
        {
            var stored = new List<UserNotificationPreference>
            {
                new UserNotificationPreference(_user.Id, NotificationType.BadgeAwarded, NotificationDeliveryChannel.EmailOnly)
            };

            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(stored.AsQueryable());

            var sut = CreateSut();
            var result = await sut.GetDeliveryChannelAsync(_user, NotificationType.BadgeAwarded);

            Assert.That(result, Is.EqualTo(NotificationDeliveryChannel.EmailOnly));
        }

        [Test]
        public async Task GetDeliveryChannelAsync_NoStoredPreference_ReturnsInAppOnly()
        {
            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<UserNotificationPreference>().AsQueryable());

            var sut = CreateSut();
            var result = await sut.GetDeliveryChannelAsync(_user, NotificationType.BadgeAwarded);

            Assert.That(result, Is.EqualTo(NotificationDeliveryChannel.InAppOnly));
        }

        #endregion

        #region SavePreferencesAsync

        [Test]
        public async Task SavePreferencesAsync_SystemCriticalEntryIgnored_NotPersisted()
        {
            var preferences = new List<(NotificationType, NotificationDeliveryChannel)>
            {
                (NotificationType.SystemCritical, NotificationDeliveryChannel.Silenced)
            };

            var sut = CreateSut();
            await sut.SavePreferencesAsync(_user, preferences);

            _mockPreferenceRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<UserNotificationPreference>()), Times.Never);
        }

        [Test]
        public async Task SavePreferencesAsync_ValidPreferences_UpsertsEachEntry()
        {
            var preferences = new List<(NotificationType, NotificationDeliveryChannel)>
            {
                (NotificationType.BadgeAwarded, NotificationDeliveryChannel.EmailOnly),
                (NotificationType.NewSightingActivity, NotificationDeliveryChannel.Silenced)
            };

            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<UserNotificationPreference>().AsQueryable());

            _mockPreferenceRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<UserNotificationPreference>()))
                .ReturnsAsync((UserNotificationPreference p) => p);

            var sut = CreateSut();
            await sut.SavePreferencesAsync(_user, preferences);

            _mockPreferenceRepo.Verify(r => r.AddOrUpdateAsync(
                It.Is<UserNotificationPreference>(p => p.NotificationType == NotificationType.BadgeAwarded
                    && p.DeliveryChannel == NotificationDeliveryChannel.EmailOnly)), Times.Once);

            _mockPreferenceRepo.Verify(r => r.AddOrUpdateAsync(
                It.Is<UserNotificationPreference>(p => p.NotificationType == NotificationType.NewSightingActivity
                    && p.DeliveryChannel == NotificationDeliveryChannel.Silenced)), Times.Once);
        }

        [Test]
        public async Task SavePreferencesAsync_ExistingPreference_UpdatesDeliveryChannel()
        {
            var existing = new UserNotificationPreference(_user.Id, NotificationType.BadgeAwarded, NotificationDeliveryChannel.InAppOnly)
            {
                Id = Guid.NewGuid()
            };

            _mockPreferenceRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<UserNotificationPreference> { existing }.AsQueryable());

            _mockPreferenceRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<UserNotificationPreference>()))
                .ReturnsAsync((UserNotificationPreference p) => p);

            var preferences = new List<(NotificationType, NotificationDeliveryChannel)>
            {
                (NotificationType.BadgeAwarded, NotificationDeliveryChannel.Silenced)
            };

            var sut = CreateSut();
            await sut.SavePreferencesAsync(_user, preferences);

            _mockPreferenceRepo.Verify(r => r.AddOrUpdateAsync(
                It.Is<UserNotificationPreference>(p => p.Id == existing.Id
                    && p.DeliveryChannel == NotificationDeliveryChannel.Silenced)), Times.Once);
        }

        #endregion
    }
}
