using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class InAppNotificationService : NotificationServiceBase<ApplicationDbContext>
    {
        public InAppNotificationService(ApplicationDbContext dbContext, ILogger<INotificationService> logger) 
            : base(dbContext, logger) { }

        public override async Task SendNotificationAsync(Notification notification)
        {
            throw new NotImplementedException();
        }
    }
}
