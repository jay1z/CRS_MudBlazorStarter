using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for managing newsletter subscriptions.
/// </summary>
public interface INewsletterService
{
    /// <summary>
    /// Subscribes an email to the newsletter and sends confirmation email.
    /// </summary>
    /// <param name="email">Email address to subscribe</param>
    /// <param name="name">Optional subscriber name</param>
    /// <param name="company">Optional company name</param>
    /// <param name="source">Where the subscription came from (e.g., "homepage", "pricing")</param>
    /// <param name="ipAddress">Subscriber's IP address for GDPR compliance</param>
    /// <param name="userAgent">User agent string for analytics</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success/failure and any messages</returns>
    Task<NewsletterSubscriptionResult> SubscribeAsync(
        string email, 
        string? name = null, 
        string? company = null,
        string? source = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Confirms a subscription using the confirmation token.
    /// </summary>
    /// <param name="token">The confirmation token from the email</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if confirmed successfully</returns>
    Task<bool> ConfirmSubscriptionAsync(Guid token, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes an email from the newsletter.
    /// </summary>
    /// <param name="email">Email to unsubscribe</param>
    /// <param name="reason">Optional reason for unsubscribing</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if unsubscribed successfully</returns>
    Task<bool> UnsubscribeAsync(string email, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes using a token (from email link).
    /// </summary>
    /// <param name="subscriberId">Subscriber ID</param>
    /// <param name="reason">Optional reason for unsubscribing</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> UnsubscribeByIdAsync(Guid subscriberId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Checks if an email is already subscribed.
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> IsSubscribedAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets a subscriber by email.
    /// </summary>
    /// <param name="email">Email to look up</param>
    /// <param name="ct">Cancellation token</param>
    Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets all active (confirmed, not unsubscribed) subscribers.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task<List<NewsletterSubscriber>> GetActiveSubscribersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets subscriber count statistics.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task<NewsletterStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Resends the confirmation email to an unconfirmed subscriber.
    /// </summary>
    /// <param name="email">Email to resend confirmation to</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> ResendConfirmationAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Updates subscriber preferences.
    /// </summary>
    /// <param name="email">Subscriber email</param>
    /// <param name="preferences">New preferences</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> UpdatePreferencesAsync(string email, NewsletterPreferences preferences, CancellationToken ct = default);

    #region Campaign Management

    /// <summary>
    /// Creates a new newsletter campaign.
    /// </summary>
    Task<NewsletterCampaign> CreateCampaignAsync(
        string name,
        string subject,
        string htmlContent,
        string? previewText = null,
        NewsletterPreferences targetPreferences = NewsletterPreferences.All,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a campaign by ID.
    /// </summary>
    Task<NewsletterCampaign?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Gets all campaigns.
    /// </summary>
    Task<List<NewsletterCampaign>> GetCampaignsAsync(bool includeDeleted = false, CancellationToken ct = default);

    /// <summary>
    /// Updates a campaign (only if still in Draft status).
    /// </summary>
    Task<NewsletterCampaign?> UpdateCampaignAsync(
        Guid campaignId,
        string? name = null,
        string? subject = null,
        string? htmlContent = null,
        string? previewText = null,
        NewsletterPreferences? targetPreferences = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a campaign immediately to all matching subscribers.
    /// </summary>
    Task<CampaignSendResult> SendCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Schedules a campaign for future sending.
    /// </summary>
    Task<bool> ScheduleCampaignAsync(Guid campaignId, DateTime scheduledAt, CancellationToken ct = default);

    /// <summary>
    /// Cancels a scheduled campaign.
    /// </summary>
    Task<bool> CancelCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a campaign (soft delete).
    /// </summary>
    Task<bool> DeleteCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Gets a preview of the campaign email.
    /// </summary>
    Task<string> GetCampaignPreviewAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Processes all scheduled campaigns that are due to be sent.
    /// Called by the background scheduler job.
    /// </summary>
    Task<ScheduledCampaignProcessResult> ProcessScheduledCampaignsAsync(CancellationToken ct = default);

    #endregion
}

/// <summary>
/// Result of processing scheduled campaigns.
/// </summary>
public record ScheduledCampaignProcessResult(
    int CampaignsProcessed,
    int TotalSent,
    int TotalFailed
);

/// <summary>
/// Result of a newsletter subscription attempt.
/// </summary>
public record NewsletterSubscriptionResult(
    bool Success,
    string Message,
    bool AlreadySubscribed = false,
    bool ConfirmationRequired = true,
    Guid? SubscriberId = null
);

/// <summary>
/// Newsletter subscription statistics.
/// </summary>
public record NewsletterStats(
    int TotalSubscribers,
    int ConfirmedSubscribers,
    int UnconfirmedSubscribers,
    int UnsubscribedCount,
    int Last30DaysNew,
    int Last30DaysUnsubscribed
);

/// <summary>
/// Result of sending a newsletter campaign.
/// </summary>
public record CampaignSendResult(
    bool Success,
    int TargetCount,
    int SentCount,
    int FailedCount,
    string? ErrorMessage = null
);
