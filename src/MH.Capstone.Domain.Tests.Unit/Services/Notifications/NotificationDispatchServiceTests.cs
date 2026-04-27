using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Notifications;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class NotificationDispatchServiceTests : NotificationServiceBaseTests<NotificationDispatchService>
    {
        private Mock<INotificationPreferenceService> _mockPreferenceService;
        private Mock<IRepository<EmailQueue, ApplicationDbContext>> _mockEmailQueueRepo;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();
            _mockPreferenceService = new Mock<INotificationPreferenceService>();
            _mockEmailQueueRepo = new Mock<IRepository<EmailQueue, ApplicationDbContext>>();
        }

        protected override NotificationDispatchService CreateSut() =>
            new NotificationDispatchService(
                _mockNotificationRepository.Object,
                _mockUserService.Object,
                NullLogger<INotificationService>.Instance,
                _mockPreferenceService.Object,
                _mockEmailQueueRepo.Object);

        #region SendNotificationAsync – SystemCritical

        [Test]
        public async Task SendNotificationAsync_SystemCritical_DeliversToInAppAndEmail()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.Is<Notification>(n => n.LinkedUserIdentityId == notif.LinkedUserIdentityId)))
                .ReturnsAsync(notif);

            _mockEmailQueueRepo
                .Setup(r => r.AddOrUpdateAsync(It.Is<EmailQueue>(e => e.Recipient == user.Email)))
                .ReturnsAsync(new EmailQueue());

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.SystemCritical);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()), Times.Once);
            _mockPreferenceService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SendNotificationAsync_WhenHtmlEmailBodySet_UsesItAsEmailBody()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);
            notif.HtmlEmailBody = "<p>Custom <a href='https://example.com'>link</a></p>";

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(notif);

            _mockEmailQueueRepo
                .Setup(r => r.AddOrUpdateAsync(It.Is<EmailQueue>(e => e.HtmlBody == notif.HtmlEmailBody)))
                .ReturnsAsync(new EmailQueue());

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.SystemCritical);

            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(
                It.Is<EmailQueue>(e => e.HtmlBody == notif.HtmlEmailBody)), Times.Once);
        }

        #endregion

        #region SendNotificationAsync – preference-based routing

        [Test]
        public async Task SendNotificationAsync_PreferenceInAppOnly_OnlyWritesToNotificationRepo()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.BadgeAwarded))
                .ReturnsAsync(NotificationDeliveryChannel.InAppOnly);

            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(notif);

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()), Times.Never);
        }

        [Test]
        public async Task SendNotificationAsync_PreferenceEmailOnly_OnlyWritesToEmailQueue()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.BadgeAwarded))
                .ReturnsAsync(NotificationDeliveryChannel.EmailOnly);

            _mockEmailQueueRepo
                .Setup(r => r.AddOrUpdateAsync(It.Is<EmailQueue>(e => e.Recipient == user.Email)))
                .ReturnsAsync(new EmailQueue());

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Never);
            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()), Times.Once);
        }

        [Test]
        public async Task SendNotificationAsync_PreferenceInAppAndEmail_WritesToBothChannels()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.NewSightingActivity))
                .ReturnsAsync(NotificationDeliveryChannel.InAppAndEmail);

            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(notif);

            _mockEmailQueueRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()))
                .ReturnsAsync(new EmailQueue());

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.NewSightingActivity);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()), Times.Once);
        }

        [Test]
        public async Task SendNotificationAsync_PreferenceSilenced_DeliversToNeitherChannel()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.ReportStatusUpdate))
                .ReturnsAsync(NotificationDeliveryChannel.Silenced);

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.ReportStatusUpdate);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Never);
            _mockEmailQueueRepo.Verify(r => r.AddOrUpdateAsync(It.IsAny<EmailQueue>()), Times.Never);
        }

        #endregion
    }
}
