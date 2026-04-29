using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly IRepository<UserNotificationPreference, ApplicationDbContext> _preferenceRepo;

        public NotificationPreferenceService(IRepository<UserNotificationPreference, ApplicationDbContext> preferenceRepo)
        {
            _preferenceRepo = preferenceRepo;
        }

        public async Task<IEnumerable<UserNotificationPreference>> GetPreferencesAsync(ApplicationUser user)
        {
            var stored = (await _preferenceRepo.GetAllAsync(p => p.UserId == user.Id)).ToList();
            return stored.Where(p => p.NotificationType != NotificationType.SystemCritical);
        }

        public async Task<NotificationDeliveryChannel> GetDeliveryChannelAsync(ApplicationUser user, NotificationType notificationType)
        {
            if (notificationType == NotificationType.SystemCritical)
                return NotificationDeliveryChannel.InAppAndEmail;

            var stored = (await _preferenceRepo.GetAllAsync(p => p.UserId == user.Id && p.NotificationType == notificationType))
                .FirstOrDefault();

            return stored?.DeliveryChannel ?? NotificationDeliveryChannel.InAppOnly;
        }

        public async Task SavePreferencesAsync(ApplicationUser user, IEnumerable<(NotificationType Type, NotificationDeliveryChannel Channel)> preferences)
        {
            var allStored = (await _preferenceRepo.GetAllAsync(p => p.UserId == user.Id)).ToList();

            foreach (var (type, channel) in preferences)
            {
                if (type == NotificationType.SystemCritical)
                    continue;

                var existing = allStored.FirstOrDefault(p => p.NotificationType == type);
                if (existing != null)
                {
                    existing.DeliveryChannel = channel;
                    await _preferenceRepo.AddOrUpdateAsync(existing);
                }
                else
                {
                    await _preferenceRepo.AddOrUpdateAsync(new UserNotificationPreference(user.Id, type, channel));
                }
            }
        }
    }
}
