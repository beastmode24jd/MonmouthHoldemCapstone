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
        /// <summary>
        /// Send a notification to a <see cref="ApplicationUser"/> that is defined within the <see cref="Notification"/> argument.
        /// </summary>
        /// <param name="notification">The <see cref="Notification"/> to send</param>
        /// <returns>A <see cref="Task"/> object representing the async task for this method</returns>
        Task SendNotificationAsync(Notification notification, NotificationType notificationType);

        //TODO - Allow for date-range filtering of notifications, and/or pagination of results for users with many notifications.
        /// <summary>
        /// Gets all pending notifications for a user with the specified id.
        /// Pending notifications are those that have not yet been read by the intended user.
        /// </summary>
        /// <param name="userId">The id of the user to get pending notifications for.</param>
        /// <returns>
        /// A <see cref="Task"/> object containing an ><see cref="IEnumerable{T}"/> of <see cref="Notification"/>
        /// that have not yet been read by the intended user. Will be empty if the user has no pending notifications.
        /// </returns>
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(Guid userId);

        /// <summary>
        /// Gets all pending notifications for the passed <see cref="ApplicationUser"/>.
        /// Pending notifications are those that have not yet been read by the intended user.
        /// </summary>
        /// <param name="user">The <see cref="ApplicationUser"/> to get pending notifications for.</param>
        /// <returns>
        /// A <see cref="Task"/> object containing an ><see cref="IEnumerable{T}"/> of <see cref="Notification"/>
        /// that have not yet been read by the intended user. Will be empty if the user has no pending notifications.
        /// </returns>
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(ApplicationUser user);

        //TODO - Allow for date-range filtering of notifications, and/or pagination of results for users with many notifications.
        /// <summary>
        /// Gets all notifications - both read and pending - for a user with the specified id.
        /// </summary>
        /// <param name="userId">The id of the user to get pending notifications for.</param>
        /// <returns>
        /// A <see cref="Task"/> object containing an ><see cref="IEnumerable{T}"/> of <see cref="Notification"/>
        /// that have been sent to the user. Will be empty if the user has no notifications.
        /// </returns>
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(Guid userId);

        /// <summary>
        /// Gets all notifications - both read and pending - for the passed <see cref="ApplicationUser"/>.
        /// </summary>
        /// <param name="user">The <see cref="ApplicationUser"/> to get pending notifications for.</param>
        /// <returns>
        /// A <see cref="Task"/> object containing an ><see cref="IEnumerable{T}"/> of <see cref="Notification"/>
        /// that have been sent to the user. Will be empty if the user has no notifications.
        /// </returns>
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(ApplicationUser user);

        /// <summary>
        /// Marks all unread notifications for the given <see cref="ApplicationUser"/> as read.
        /// </summary>
        /// <param name="user">The <see cref="ApplicationUser"/> whose unread notifications should be marked as read.</param>
        /// <returns>A <see cref="Task"/> representing the async operation.</returns>
        Task MarkAllAsReadAsync(ApplicationUser user);

        /// <summary>
        /// Permanently deletes all notifications for the given <see cref="ApplicationUser"/>.
        /// </summary>
        /// <param name="user">The <see cref="ApplicationUser"/> whose notifications should be deleted.</param>
        /// <returns>A <see cref="Task"/> representing the async operation.</returns>
        Task DeleteAllAsync(ApplicationUser user);
    }
}
