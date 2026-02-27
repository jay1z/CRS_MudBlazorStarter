using Horizon.Models;

namespace Horizon.Services.Interfaces {
    public interface ISignalRService {
        Task NotifyTaskUpdated(Guid reserveStudyId);
        Task NotifyTaskModified(Guid reserveStudyId, KanbanTask task);

        /// <summary>
        /// Sends a notification to a specific user.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="notification">The notification to send</param>
        Task SendNotificationToUserAsync(Guid userId, Notification notification);

        /// <summary>
        /// Notifies a user about a new notification count.
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="unreadCount">The count of unread notifications</param>
        Task NotifyUnreadCountAsync(Guid userId, int unreadCount);
    }
}
