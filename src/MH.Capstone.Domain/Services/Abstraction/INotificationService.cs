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
        Task SendNotificationAsync(Notification notification);

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
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(ApplicationUser user)
            => GetPendingNotificationsAsync(user.GuidId);

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
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(ApplicationUser user)
            => GetAllNotificationsAsync(user.GuidId);

        /// <summary>
        /// Marks a notification as read by the user.
        /// </summary>
        /// <param name="notificationId">The id of the notification to mark read.</param>
        /// <returns>A <see cref="Task"/> object representing the async task for this method</returns>
        Task MarkNotificationAsReadAsync(Guid notificationId);

        /// <summary>
        /// Marks a notification as read by the user.
        /// </summary>
        /// <param name="notification">The notification to mark read.</param>
        /// <returns>A <see cref="Task"/> object representing the async task for this method</returns>
        Task MarkNotificationAsReadAsync(Notification notification)
            => MarkNotificationAsReadAsync(notification.Id);

        /// <summary>
        /// Marks each notification as read by the user in the provided <see cref="IEnumerable{T}"/> of notification ids.
        /// </summary>
        /// <param name="notificationIds">The <see cref="IEnumerable{T}"/> of notification ids to mark read by the user.</param>
        /// <returns>A <see cref="Task"/> object representing the async task for this method</returns>
        Task MarkNotificationsAsReadAsync(IEnumerable<Guid> notificationIds)
            // One line implementation that calls the single notification overload of this method for each notification id 
            // in the list, and waits for all of those tasks to complete before returning.
            => Task.WhenAll(notificationIds.Select(MarkNotificationAsReadAsync));

        /// <summary>
        /// Marks each notification as read by the user in the provided <see cref="IEnumerable{T}"/> of notifications.
        /// </summary>
        /// <param name="notifications">The <see cref="IEnumerable{T}"/> of <see cref="Notification"/> to mark read by the user.</param>
        /// <returns>A <see cref="Task"/> object representing the async task for this method</returns>
        Task MarkNotificationsAsReadAsync(IEnumerable<Notification> notifications)
            // Select the Id of each notification and pass that new list of ids the other overload of this method.
            => MarkNotificationsAsReadAsync(notifications.Select(n => n.Id));
    }
}
