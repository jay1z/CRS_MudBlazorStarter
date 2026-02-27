using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for managing in-app notifications for users.
/// </summary>
public interface IAppNotificationService
{
    /// <summary>
    /// Creates a notification for a user.
    /// </summary>
    Task<Notification> CreateAsync(Notification notification, CancellationToken ct = default);

    /// <summary>
    /// Creates a simple notification for a user.
    /// </summary>
    Task<Notification> NotifyAsync(
        Guid userId,
        string message,
        string? title = null,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionUrl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a notification linked to an entity.
    /// </summary>
    Task<Notification> NotifyWithEntityAsync(
        Guid userId,
        string message,
        string entityType,
        Guid entityId,
        string? title = null,
        NotificationType type = NotificationType.Info,
        string? actionUrl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies when a message is received.
    /// </summary>
    Task NotifyMessageReceivedAsync(Guid toUserId, Guid messageId);

    /// <summary>
    /// Gets unread notifications for a user.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetUnreadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all notifications for a user.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetAllAsync(Guid userId, int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets the unread count for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a notification.
    /// </summary>
    Task<bool> DeleteAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Deletes all read notifications older than the specified date.
    /// </summary>
    Task<int> CleanupOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default);
}