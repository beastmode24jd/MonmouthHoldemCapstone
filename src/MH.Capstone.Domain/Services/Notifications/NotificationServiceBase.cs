using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Notifications
{
    public abstract class NotificationServiceBase<TDbContext> : INotificationService where TDbContext : DbContext
    {
        protected readonly TDbContext _dbContext;
        protected readonly ILogger<INotificationService> _logger;

        protected NotificationServiceBase(TDbContext dbContext, ILogger<INotificationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Abstract method as the actual implementation will depend on the specific type of notification
        // service (ie email, in-app, sms, etc.)
        public abstract Task SendNotificationAsync(Notification notification);

        public virtual async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<IEnumerable<Notification>> GetAllNotificationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public virtual async Task MarkNotificationAsReadAsync(Guid notificationId)
        {
            throw new NotImplementedException();
        }
    }
}
