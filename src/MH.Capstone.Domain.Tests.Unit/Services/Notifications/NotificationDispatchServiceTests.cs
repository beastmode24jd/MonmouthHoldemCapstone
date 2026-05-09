using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
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
        private Channel<EmailMessage> _emailChannel;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();
            _mockPreferenceService = new Mock<INotificationPreferenceService>();
            _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        }

        protected override NotificationDispatchService CreateSut() =>
            new NotificationDispatchService(
                _mockNotificationRepository.Object,
                _mockUserService.Object,
                NullLogger<INotificationService>.Instance,
                _mockPreferenceService.Object,
                _emailChannel.Writer);

        private async Task<EmailMessage?> TryReadEmailAsync()
        {
            _emailChannel.Writer.TryComplete();
            return await _emailChannel.Reader.ReadAsync().AsTask()
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : (EmailMessage?)null);
        }

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

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.SystemCritical);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            _mockPreferenceService.VerifyNoOtherCalls();

            Assert.That(_emailChannel.Reader.TryRead(out var queued), Is.True);
            Assert.That(queued!.Recipient, Is.EqualTo(user.Email));
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

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.SystemCritical);

            Assert.That(_emailChannel.Reader.TryRead(out var queued), Is.True);
            Assert.That(queued!.HtmlBody, Is.EqualTo(notif.HtmlEmailBody));
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
            Assert.That(_emailChannel.Reader.TryRead(out _), Is.False);
        }

        [Test]
        public async Task SendNotificationAsync_PreferenceEmailOnly_OnlyWritesToEmailChannel()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.BadgeAwarded))
                .ReturnsAsync(NotificationDeliveryChannel.EmailOnly);

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.BadgeAwarded);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Never);
            Assert.That(_emailChannel.Reader.TryRead(out var queued), Is.True);
            Assert.That(queued!.Recipient, Is.EqualTo(user.Email));
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

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.NewSightingActivity);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            Assert.That(_emailChannel.Reader.TryRead(out var queued), Is.True);
            Assert.That(queued!.Recipient, Is.EqualTo(user.Email));
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
            Assert.That(_emailChannel.Reader.TryRead(out _), Is.False);
        }

        #endregion

        #region SendNotificationAsync – AccountActivity (preference-driven, not system-critical)

        [Test]
        public async Task SendNotificationAsync_AccountActivity_ConsultsPreferencesInsteadOfDefaultingToInAppAndEmail()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.AccountActivity))
                .ReturnsAsync(NotificationDeliveryChannel.Silenced);

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.AccountActivity);

            _mockPreferenceService.Verify(s => s.GetDeliveryChannelAsync(user, NotificationType.AccountActivity), Times.Once);
            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Never);
            Assert.That(_emailChannel.Reader.TryRead(out _), Is.False);
        }

        [Test]
        public async Task SendNotificationAsync_AccountActivity_InAppAndEmailFlag_DeliversToBothChannels()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
            var notif = NotificationValidValuesSource.GetValidNotification(user.GuidId);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            _mockPreferenceService
                .Setup(s => s.GetDeliveryChannelAsync(user, NotificationType.AccountActivity))
                .ReturnsAsync(NotificationDeliveryChannel.InAppAndEmail);

            _mockNotificationRepository
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(notif);

            var sut = CreateSut();
            await sut.SendNotificationAsync(notif, NotificationType.AccountActivity);

            _mockNotificationRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<Notification>()), Times.Once);
            Assert.That(_emailChannel.Reader.TryRead(out var queued), Is.True);
            Assert.That(queued!.Recipient, Is.EqualTo(user.Email));
        }

        #endregion
    }
}
