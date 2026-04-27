using System.Collections.Generic;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface INotificationPreferenceService
    {
        /// <summary>
        /// Gets all configurable notification preferences for the user.
        /// SystemCritical is excluded — it is always InAppAndEmail and not user-configurable.
        /// </summary>
        Task<IEnumerable<UserNotificationPreference>> GetPreferencesAsync(ApplicationUser user);

        /// <summary>
        /// Gets the delivery channel for a specific notification type for the user.
        /// If no preference is stored, returns the system default (InAppOnly).
        /// SystemCritical always returns InAppAndEmail regardless of stored preferences.
        /// </summary>
        Task<NotificationDeliveryChannel> GetDeliveryChannelAsync(ApplicationUser user, NotificationType notificationType);

        /// <summary>
        /// Saves (upserts) the given preferences for the user.
        /// Any attempt to update the SystemCritical type is silently ignored.
        /// </summary>
        Task SavePreferencesAsync(ApplicationUser user, IEnumerable<(NotificationType Type, NotificationDeliveryChannel Channel)> preferences);
    }
}
