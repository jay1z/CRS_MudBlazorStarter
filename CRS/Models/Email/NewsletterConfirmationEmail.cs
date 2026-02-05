namespace CRS.Models.Email;

/// <summary>
/// Model for newsletter confirmation emails.
/// </summary>
public class NewsletterConfirmationEmail
{
    /// <summary>
    /// Subscriber's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Subscriber's name (if provided).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// URL to confirm the subscription.
    /// </summary>
    public required string ConfirmationUrl { get; set; }

    /// <summary>
    /// URL to unsubscribe.
    /// </summary>
    public required string UnsubscribeUrl { get; set; }
}
