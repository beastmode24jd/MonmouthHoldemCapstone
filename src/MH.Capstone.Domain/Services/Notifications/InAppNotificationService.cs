using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Notifications
{
    public class InAppNotificationService : NotificationServiceBase
    {
        public InAppNotificationService(IRepository<Notification, ApplicationDbContext> repo,
            IUserService userService, ILogger<INotificationService> logger) 
            : base(repo, userService, logger) { }

        public override async Task SendNotificationAsync(Notification notification)
        {
            if (!notification.TryValidateEntity(out var errors))
            {
                var errList = errors.ToList();
                var errPropNames = string.Empty;
                errList.ForEach(prop => errPropNames += $" {prop}");
                throw new ArgumentException($"The argument {nameof(notification)} has {errList.Count} " +
                                            $"data annotation violations...\n" +
                                            $"\tInvalid Properties: {errPropNames}", 
                    nameof(notification));
            }

            // Ensure the notification is marked as unread when sent
            notification.IsRead = false;

            await HandleSqlErrors(_notificationRepo.AddOrUpdateAsync(notification),nameof(notification));
        }
    }
}
