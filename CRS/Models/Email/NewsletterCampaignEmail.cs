namespace Horizon.Models.Email;

/// <summary>
/// Model for newsletter campaign emails.
/// </summary>
public class NewsletterCampaignEmail
{
    /// <summary>
    /// Campaign ID for tracking.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Preview text shown in email clients.
    /// </summary>
    public string? PreviewText { get; set; }

    /// <summary>
    /// HTML content of the newsletter.
    /// </summary>
    public required string HtmlContent { get; set; }

    /// <summary>
    /// Subscriber's email address.
    /// </summary>
    public required string SubscriberEmail { get; set; }

    /// <summary>
    /// Subscriber's name (if provided).
    /// </summary>
    public string? SubscriberName { get; set; }

    /// <summary>
    /// URL to unsubscribe.
    /// </summary>
    public required string UnsubscribeUrl { get; set; }
}
