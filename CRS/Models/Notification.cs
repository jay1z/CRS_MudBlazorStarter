using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Represents an in-app notification for a user.
/// Notifications are tenant-scoped and targeted to specific users.
/// </summary>
public class Notification : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Target user
    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Content
    [MaxLength(200)]
    public string? Title { get; set; }

    [Required]
    [DataType(DataType.Text)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    // Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Optional link to related entity for deep linking
    [MaxLength(100)]
    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    // For grouping/filtering
    [MaxLength(50)]
    public string? Category { get; set; }
}

/// <summary>
/// Types of notifications for visual styling and filtering.
/// </summary>
public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Action = 4
}

/// <summary>
/// Priority levels for notifications.
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
