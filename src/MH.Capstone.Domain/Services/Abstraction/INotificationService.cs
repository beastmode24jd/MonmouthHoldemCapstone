using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Notification notification);

        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(Guid userId);

        Task<IEnumerable<Notification>> GetAllNotificationsAsync(Guid userId);

        Task MarkNotificationAsReadAsync(Guid notificationId);

        async Task MarkNotificationsAsReadAsync(IEnumerable<Guid> notificationIds)
        {
            foreach (var nid in notificationIds)
            {
                await MarkNotificationAsReadAsync(nid);
            }
        }
    }
}
