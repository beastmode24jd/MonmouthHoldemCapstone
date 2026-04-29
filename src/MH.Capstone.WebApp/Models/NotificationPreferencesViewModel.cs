using System.Collections.Generic;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class NotificationPreferenceEntryViewModel
    {
        public NotificationType NotificationType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NotificationDeliveryChannel SelectedChannel { get; set; } = NotificationDeliveryChannel.InAppOnly;
    }

    public class NotificationPreferencesViewModel
    {
        public List<NotificationPreferenceEntryViewModel> Preferences { get; set; } = new();
    }
}
