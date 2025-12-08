using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Tracks all emails sent by the system for compliance, debugging, and audit purposes.
/// </summary>
public class EmailLog : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Optional association to reserve study
    [ForeignKey(nameof(ReserveStudy))]
    public Guid? ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Email details
    [Required]
    [MaxLength(256)]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? CcEmails { get; set; }

    [MaxLength(1024)]
    public string? BccEmails { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TemplateType { get; set; }

    // Status tracking
    public EmailStatus Status { get; set; } = EmailStatus.Queued;

    public DateTime? QueuedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime? FailedAt { get; set; }

    // Error handling
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;

    // External tracking
    [MaxLength(256)]
    public string? ExternalMessageId { get; set; }

    [MaxLength(50)]
    public string? EmailProvider { get; set; }
}

/// <summary>
/// Status of an email in the delivery pipeline.
/// </summary>
public enum EmailStatus
{
    Queued = 0,
    Sending = 1,
    Sent = 2,
    Delivered = 3,
    Opened = 4,
    Clicked = 5,
    Bounced = 6,
    Failed = 7,
    Unsubscribed = 8,
    SpamComplaint = 9
}
