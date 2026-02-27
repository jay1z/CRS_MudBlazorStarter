using System.ComponentModel.DataAnnotations;

namespace Horizon.Models;

/// <summary>
/// Represents a newsletter subscriber for marketing emails.
/// Supports double opt-in confirmation flow.
/// </summary>
public class NewsletterSubscriber
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Subscriber's email address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Subscriber's name (optional).
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// Company or organization name (optional).
    /// </summary>
    [MaxLength(200)]
    public string? Company { get; set; }

    /// <summary>
    /// Source of the subscription (e.g., "homepage", "pricing", "demo-request").
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// IP address of the subscriber (for GDPR compliance).
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (for analytics).
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Token used for email confirmation (double opt-in).
    /// </summary>
    public Guid? ConfirmationToken { get; set; }

    /// <summary>
    /// When the confirmation token expires.
    /// </summary>
    public DateTime? ConfirmationTokenExpiresAt { get; set; }

    /// <summary>
    /// Whether the email has been confirmed (double opt-in complete).
    /// </summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    /// When the subscription was confirmed.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Whether the subscriber has unsubscribed.
    /// </summary>
    public bool IsUnsubscribed { get; set; } = false;

    /// <summary>
    /// When the subscriber unsubscribed.
    /// </summary>
    public DateTime? UnsubscribedAt { get; set; }

    /// <summary>
    /// Unsubscribe reason (optional feedback).
    /// </summary>
    [MaxLength(500)]
    public string? UnsubscribeReason { get; set; }

    /// <summary>
    /// Newsletter preferences/interests.
    /// </summary>
    public NewsletterPreferences Preferences { get; set; } = NewsletterPreferences.All;

    /// <summary>
    /// When the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// External provider subscriber ID (e.g., SendGrid contact ID, Mailchimp subscriber ID).
    /// </summary>
    [MaxLength(100)]
    public string? ExternalProviderId { get; set; }

    /// <summary>
    /// Which provider is managing this subscriber.
    /// </summary>
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Generates a new confirmation token valid for 24 hours.
    /// </summary>
    public void GenerateConfirmationToken()
    {
        ConfirmationToken = Guid.NewGuid();
        ConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Confirms the subscription if the token is valid.
    /// </summary>
    public bool Confirm(Guid token)
    {
        if (ConfirmationToken == token && 
            ConfirmationTokenExpiresAt.HasValue && 
            ConfirmationTokenExpiresAt > DateTime.UtcNow)
        {
            IsConfirmed = true;
            ConfirmedAt = DateTime.UtcNow;
            ConfirmationToken = null;
            ConfirmationTokenExpiresAt = null;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Unsubscribes the user.
    /// </summary>
    public void Unsubscribe(string? reason = null)
    {
        IsUnsubscribed = true;
        UnsubscribedAt = DateTime.UtcNow;
        UnsubscribeReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resubscribes a previously unsubscribed user.
    /// </summary>
    public void Resubscribe()
    {
        IsUnsubscribed = false;
        UnsubscribedAt = null;
        UnsubscribeReason = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Newsletter content preferences.
/// </summary>
[Flags]
public enum NewsletterPreferences
{
    None = 0,
    ProductUpdates = 1,
    IndustryNews = 2,
    Tips = 4,
    Webinars = 8,
    Promotions = 16,
    All = ProductUpdates | IndustryNews | Tips | Webinars | Promotions
}
