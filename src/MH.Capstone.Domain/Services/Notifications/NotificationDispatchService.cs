using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class NotificationDispatchService : NotificationServiceBase
    {
        private readonly INotificationPreferenceService _preferenceService;
        private readonly ChannelWriter<EmailMessage> _emailChannel;

        public NotificationDispatchService(
            IRepository<Notification, ApplicationDbContext> notificationRepo,
            IUserService userService,
            ILogger<INotificationService> logger,
            INotificationPreferenceService preferenceService,
            ChannelWriter<EmailMessage> emailChannel)
            : base(notificationRepo, userService, logger)
        {
            _preferenceService = preferenceService;
            _emailChannel = emailChannel;
        }

        public override async Task SendNotificationAsync(Notification notification, NotificationType notificationType)
        {
            var user = await _userService.GetUserByIdAsync(notification.RecipientId);
            if (user == null)
            {
                _logger.LogWarning("Notification dispatch skipped: user {UserId} not found", notification.RecipientId);
                return;
            }

            NotificationDeliveryChannel channel;
            if (notificationType == NotificationType.SystemCritical)
            {
                channel = NotificationDeliveryChannel.InAppAndEmail;
            }
            else
            {
                channel = await _preferenceService.GetDeliveryChannelAsync(user, notificationType);
            }

            if (channel.HasFlag(NotificationDeliveryChannel.InAppOnly))
                await DeliverInAppAsync(notification);

            if (channel.HasFlag(NotificationDeliveryChannel.EmailOnly))
                await DeliverEmailAsync(user, notification);
        }

        private async Task DeliverInAppAsync(Notification notification)
        {
            notification.IsRead = false;
            await HandleSqlErrors(_notificationRepo.AddOrUpdateAsync(notification), nameof(notification));
        }

        private async Task DeliverEmailAsync(ApplicationUser user, Notification notification)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return;

            var message = new EmailMessage(
                user.Email,
                notification.Title,
                notification.HtmlEmailBody ?? $"<p>{notification.Message}</p>");

            await _emailChannel.WriteAsync(message);
        }
    }
}
