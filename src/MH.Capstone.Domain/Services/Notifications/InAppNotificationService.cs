using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class InAppNotificationService : INotificationService
    {
        public async Task SendNotificationAsync(Notification notification)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Notification>> GetAllNotificationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public async Task MarkNotificationAsReadAsync(Guid notificationId)
        {
            throw new NotImplementedException();
        }
    }
}
