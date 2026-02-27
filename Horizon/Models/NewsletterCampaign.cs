using System.ComponentModel.DataAnnotations;

namespace Horizon.Models;

/// <summary>
/// Represents a newsletter campaign that can be sent to subscribers.
/// </summary>
public class NewsletterCampaign
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Campaign name for internal reference.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Preview text shown in email clients.
    /// </summary>
    [MaxLength(300)]
    public string? PreviewText { get; set; }

    /// <summary>
    /// HTML content of the newsletter.
    /// </summary>
    [Required]
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Plain text version (auto-generated if not provided).
    /// </summary>
    public string? PlainTextContent { get; set; }

    /// <summary>
    /// Current status of the campaign.
    /// </summary>
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    /// <summary>
    /// Target audience preferences filter.
    /// </summary>
    public NewsletterPreferences TargetPreferences { get; set; } = NewsletterPreferences.All;

    /// <summary>
    /// When the campaign was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the campaign was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the campaign was scheduled to send (null = send immediately).
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// When the campaign started sending.
    /// </summary>
    public DateTime? SendingStartedAt { get; set; }

    /// <summary>
    /// When the campaign finished sending.
    /// </summary>
    public DateTime? SendingCompletedAt { get; set; }

    /// <summary>
    /// Number of subscribers targeted.
    /// </summary>
    public int TargetCount { get; set; }

    /// <summary>
    /// Number of emails successfully sent.
    /// </summary>
    public int SentCount { get; set; }

    /// <summary>
    /// Number of emails that failed to send.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of emails opened (if tracking enabled).
    /// </summary>
    public int OpenCount { get; set; }

    /// <summary>
    /// Number of link clicks (if tracking enabled).
    /// </summary>
    public int ClickCount { get; set; }

    /// <summary>
    /// Number of unsubscribes from this campaign.
    /// </summary>
    public int UnsubscribeCount { get; set; }

    /// <summary>
    /// External provider campaign ID (for future SendGrid/Mailchimp integration).
    /// </summary>
    [MaxLength(100)]
    public string? ExternalCampaignId { get; set; }

    /// <summary>
    /// External provider name if sent via integration.
    /// </summary>
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Error message if sending failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Soft delete timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Status of a newsletter campaign.
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Campaign is being drafted.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Campaign is scheduled for future sending.
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Campaign is currently being sent.
    /// </summary>
    Sending = 2,

    /// <summary>
    /// Campaign has been sent successfully.
    /// </summary>
    Sent = 3,

    /// <summary>
    /// Campaign sending failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Campaign was cancelled.
    /// </summary>
    Cancelled = 5
}
