using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for managing in-app notifications for users.
/// </summary>
public class AppNotificationService : IAppNotificationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public AppNotificationService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<Notification> CreateAsync(Notification notification, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        notification.TenantId = _tenantContext.TenantId.Value;
        notification.DateCreated = DateTime.UtcNow;
        notification.IsRead = false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(ct);

        // TODO: Add SignalR push notification when SendToUserAsync is available
        // if (_signalRService != null)
        // {
        //     await _signalRService.SendToUserAsync(notification.UserId.ToString(), "NotificationReceived", notification);
        // }

        return notification;
    }

    public async Task<Notification> NotifyAsync(
        Guid userId,
        string message,
        string? title = null,
        NotificationType type = NotificationType.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionUrl = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Title = title,
            Type = type,
            Priority = priority,
            ActionUrl = actionUrl
        };

        return await CreateAsync(notification, ct);
    }

    public async Task<Notification> NotifyWithEntityAsync(
        Guid userId,
        string message,
        string entityType,
        Guid entityId,
        string? title = null,
        NotificationType type = NotificationType.Info,
        string? actionUrl = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Title = title,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            ActionUrl = actionUrl
        };

        return await CreateAsync(notification, ct);
    }

    public async Task NotifyMessageReceivedAsync(Guid toUserId, Guid messageId)
    {
        if (!_tenantContext.TenantId.HasValue) return;

        await NotifyWithEntityAsync(
            toUserId,
            "You have received a new message",
            "Message",
            messageId,
            title: "New Message",
            type: NotificationType.Info,
            actionUrl: $"/Messages/{messageId}");
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(Guid userId, int count = 50, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.DateDeleted == null, ct);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.DateDeleted == null, ct);

        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var unreadNotifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.DateDeleted == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.DateModified = now;
        }

        await context.SaveChangesAsync(ct);
        return unreadNotifications.Count;
    }

    public async Task<bool> DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

        if (notification == null) return false;

        notification.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> CleanupOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var oldNotifications = await context.Notifications
            .Where(n => n.IsRead && n.DateCreated < olderThan && n.DateDeleted == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var notification in oldNotifications)
        {
            notification.DateDeleted = now;
        }

        await context.SaveChangesAsync(ct);
        return oldNotifications.Count;
    }
}