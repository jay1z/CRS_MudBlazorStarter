using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRS.Services;

/// <summary>
/// Service for managing newsletter subscriptions with double opt-in support.
/// </summary>
public class NewsletterService : INewsletterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IMailer _mailer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailer mailer,
        IConfiguration configuration,
        ILogger<NewsletterService> logger)
    {
        _dbFactory = dbFactory;
        _mailer = mailer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<NewsletterSubscriptionResult> SubscribeAsync(
        string email,
        string? name = null,
        string? company = null,
        string? source = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Check for existing subscription
        var existing = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email, ct);

        if (existing != null)
        {
            // Already confirmed and active
            if (existing.IsConfirmed && !existing.IsUnsubscribed)
            {
                _logger.LogInformation("Email {Email} is already subscribed", email);
                return new NewsletterSubscriptionResult(
                    Success: true,
                    Message: "You're already subscribed to our newsletter!",
                    AlreadySubscribed: true,
                    ConfirmationRequired: false,
                    SubscriberId: existing.Id
                );
            }

            // Previously unsubscribed - resubscribe
            if (existing.IsUnsubscribed)
            {
                existing.Resubscribe();
                existing.GenerateConfirmationToken();
                existing.Source = source ?? existing.Source;
                existing.IpAddress = ipAddress;
                existing.UserAgent = userAgent;
                
                await context.SaveChangesAsync(ct);
                await SendConfirmationEmailAsync(existing);
                
                _logger.LogInformation("Resubscribed {Email} - confirmation email sent", email);
                return new NewsletterSubscriptionResult(
                    Success: true,
                    Message: "Welcome back! Please check your email to confirm your subscription.",
                    ConfirmationRequired: true,
                    SubscriberId: existing.Id
                );
            }

            // Not yet confirmed - resend confirmation
            if (!existing.IsConfirmed)
            {
                existing.GenerateConfirmationToken();
                existing.UpdatedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync(ct);
                await SendConfirmationEmailAsync(existing);
                
                _logger.LogInformation("Resent confirmation to {Email}", email);
                return new NewsletterSubscriptionResult(
                    Success: true,
                    Message: "We've sent you another confirmation email. Please check your inbox.",
                    ConfirmationRequired: true,
                    SubscriberId: existing.Id
                );
            }
        }

        // New subscription
        var subscriber = new NewsletterSubscriber
        {
            Email = email,
            Name = name,
            Company = company,
            Source = source ?? "website",
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        subscriber.GenerateConfirmationToken();

        context.NewsletterSubscribers.Add(subscriber);
        await context.SaveChangesAsync(ct);

        // Send confirmation email
        await SendConfirmationEmailAsync(subscriber);

        _logger.LogInformation("New newsletter subscription from {Email}, source: {Source}", email, source);

        return new NewsletterSubscriptionResult(
            Success: true,
            Message: "Thanks for subscribing! Please check your email to confirm your subscription.",
            ConfirmationRequired: true,
            SubscriberId: subscriber.Id
        );
    }

    public async Task<bool> ConfirmSubscriptionAsync(Guid token, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.ConfirmationToken == token, ct);

        if (subscriber == null)
        {
            _logger.LogWarning("Confirmation attempt with invalid token: {Token}", token);
            return false;
        }

        if (subscriber.Confirm(token))
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Newsletter subscription confirmed for {Email}", subscriber.Email);
            return true;
        }

        _logger.LogWarning("Confirmation token expired for {Email}", subscriber.Email);
        return false;
    }

    public async Task<bool> UnsubscribeAsync(string email, string? reason = null, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email, ct);

        if (subscriber == null)
        {
            return false;
        }

        subscriber.Unsubscribe(reason);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Unsubscribed {Email}, reason: {Reason}", email, reason ?? "not provided");
        return true;
    }

    public async Task<bool> UnsubscribeByIdAsync(Guid subscriberId, string? reason = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Id == subscriberId, ct);

        if (subscriber == null)
        {
            return false;
        }

        subscriber.Unsubscribe(reason);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Unsubscribed {Email} by ID, reason: {Reason}", subscriber.Email, reason ?? "not provided");
        return true;
    }

    public async Task<bool> IsSubscribedAsync(string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.NewsletterSubscribers
            .AnyAsync(s => s.Email == email && s.IsConfirmed && !s.IsUnsubscribed, ct);
    }

    public async Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email, ct);
    }

    public async Task<List<NewsletterSubscriber>> GetActiveSubscribersAsync(CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.NewsletterSubscribers
            .Where(s => s.IsConfirmed && !s.IsUnsubscribed)
            .OrderByDescending(s => s.ConfirmedAt)
            .ToListAsync(ct);
    }

    public async Task<NewsletterStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var total = await context.NewsletterSubscribers.CountAsync(ct);
        var confirmed = await context.NewsletterSubscribers.CountAsync(s => s.IsConfirmed && !s.IsUnsubscribed, ct);
        var unconfirmed = await context.NewsletterSubscribers.CountAsync(s => !s.IsConfirmed && !s.IsUnsubscribed, ct);
        var unsubscribed = await context.NewsletterSubscribers.CountAsync(s => s.IsUnsubscribed, ct);
        var newLast30 = await context.NewsletterSubscribers.CountAsync(s => s.CreatedAt >= thirtyDaysAgo, ct);
        var unsubLast30 = await context.NewsletterSubscribers.CountAsync(s => s.UnsubscribedAt >= thirtyDaysAgo, ct);

        return new NewsletterStats(
            TotalSubscribers: total,
            ConfirmedSubscribers: confirmed,
            UnconfirmedSubscribers: unconfirmed,
            UnsubscribedCount: unsubscribed,
            Last30DaysNew: newLast30,
            Last30DaysUnsubscribed: unsubLast30
        );
    }

    public async Task<bool> ResendConfirmationAsync(string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email && !s.IsConfirmed, ct);

        if (subscriber == null)
        {
            return false;
        }

        subscriber.GenerateConfirmationToken();
        subscriber.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        await SendConfirmationEmailAsync(subscriber);

        _logger.LogInformation("Resent confirmation email to {Email}", email);
        return true;
    }

    public async Task<bool> UpdatePreferencesAsync(string email, NewsletterPreferences preferences, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email, ct);

        if (subscriber == null)
        {
            return false;
        }

        subscriber.Preferences = preferences;
        subscriber.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    private async Task SendConfirmationEmailAsync(NewsletterSubscriber subscriber)
    {
        try
        {
            var baseUrl = _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://reservecloud.com";
            var confirmUrl = $"{baseUrl}/newsletter/confirm?token={subscriber.ConfirmationToken}";
            var unsubscribeUrl = $"{baseUrl}/newsletter/unsubscribe?id={subscriber.Id}";

            var emailModel = new NewsletterConfirmationEmail
            {
                Email = subscriber.Email,
                Name = subscriber.Name,
                ConfirmationUrl = confirmUrl,
                UnsubscribeUrl = unsubscribeUrl
            };

            var mailable = new NewsletterConfirmationMailable(emailModel);
            await _mailer.SendAsync(mailable);

            _logger.LogDebug("Sent confirmation email to {Email}", subscriber.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", subscriber.Email);
            // Don't throw - the subscription is saved, email can be resent
        }
    }

    #region Campaign Management

    public async Task<NewsletterCampaign> CreateCampaignAsync(
        string name,
        string subject,
        string htmlContent,
        string? previewText = null,
        NewsletterPreferences targetPreferences = NewsletterPreferences.All,
        CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = new NewsletterCampaign
        {
            Name = name,
            Subject = subject,
            HtmlContent = htmlContent,
            PreviewText = previewText,
            TargetPreferences = targetPreferences,
            Status = CampaignStatus.Draft
        };

        context.NewsletterCampaigns.Add(campaign);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Created newsletter campaign: {Name} ({Id})", name, campaign.Id);
        return campaign;
    }

    public async Task<NewsletterCampaign?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);
    }

    public async Task<List<NewsletterCampaign>> GetCampaignsAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var query = context.NewsletterCampaigns.AsQueryable();
        if (!includeDeleted)
        {
            query = query.Where(c => c.DeletedAt == null);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<NewsletterCampaign?> UpdateCampaignAsync(
        Guid campaignId,
        string? name = null,
        string? subject = null,
        string? htmlContent = null,
        string? previewText = null,
        NewsletterPreferences? targetPreferences = null,
        CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);

        if (campaign == null || campaign.Status != CampaignStatus.Draft)
        {
            return null;
        }

        if (name != null) campaign.Name = name;
        if (subject != null) campaign.Subject = subject;
        if (htmlContent != null) campaign.HtmlContent = htmlContent;
        if (previewText != null) campaign.PreviewText = previewText;
        if (targetPreferences.HasValue) campaign.TargetPreferences = targetPreferences.Value;
        campaign.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task<CampaignSendResult> SendCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);

        if (campaign == null)
        {
            return new CampaignSendResult(false, 0, 0, 0, "Campaign not found");
        }

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
        {
            return new CampaignSendResult(false, 0, 0, 0, "Campaign has already been sent or cancelled");
        }

        // Get target subscribers
        var subscribers = await context.NewsletterSubscribers
            .Where(s => s.IsConfirmed && !s.IsUnsubscribed)
            .Where(s => (s.Preferences & campaign.TargetPreferences) != 0 || campaign.TargetPreferences == NewsletterPreferences.All)
            .ToListAsync(ct);

        if (subscribers.Count == 0)
        {
            return new CampaignSendResult(false, 0, 0, 0, "No subscribers match the target criteria");
        }

        // Update campaign status
        campaign.Status = CampaignStatus.Sending;
        campaign.SendingStartedAt = DateTime.UtcNow;
        campaign.TargetCount = subscribers.Count;
        await context.SaveChangesAsync(ct);

        var baseUrl = _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://reservecloud.com";
        int sentCount = 0;
        int failedCount = 0;

        foreach (var subscriber in subscribers)
        {
            try
            {
                var unsubscribeUrl = $"{baseUrl}/newsletter/unsubscribe?id={subscriber.Id}";

                var emailModel = new NewsletterCampaignEmail
                {
                    CampaignId = campaign.Id,
                    Subject = campaign.Subject,
                    PreviewText = campaign.PreviewText,
                    HtmlContent = campaign.HtmlContent,
                    SubscriberEmail = subscriber.Email,
                    SubscriberName = subscriber.Name,
                    UnsubscribeUrl = unsubscribeUrl
                };

                var mailable = new NewsletterCampaignMailable(emailModel);
                await _mailer.SendAsync(mailable);

                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send campaign {CampaignId} to {Email}", campaignId, subscriber.Email);
                failedCount++;
            }
        }

        // Update final status
        campaign.Status = failedCount == subscribers.Count ? CampaignStatus.Failed : CampaignStatus.Sent;
        campaign.SendingCompletedAt = DateTime.UtcNow;
        campaign.SentCount = sentCount;
        campaign.FailedCount = failedCount;

        if (campaign.Status == CampaignStatus.Failed)
        {
            campaign.ErrorMessage = $"All {failedCount} emails failed to send";
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Campaign {CampaignId} sent: {Sent}/{Total} successful, {Failed} failed",
            campaignId, sentCount, subscribers.Count, failedCount);

        return new CampaignSendResult(
            Success: campaign.Status == CampaignStatus.Sent,
            TargetCount: subscribers.Count,
            SentCount: sentCount,
            FailedCount: failedCount,
            ErrorMessage: campaign.ErrorMessage
        );
    }

    public async Task<bool> ScheduleCampaignAsync(Guid campaignId, DateTime scheduledAt, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);

        if (campaign == null || campaign.Status != CampaignStatus.Draft)
        {
            return false;
        }

        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = scheduledAt;
        campaign.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Scheduled campaign {CampaignId} for {ScheduledAt}", campaignId, scheduledAt);
        return true;
    }

    public async Task<bool> CancelCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);

        if (campaign == null)
        {
            return false;
        }

        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
        {
            return false;
        }

        campaign.Status = CampaignStatus.Cancelled;
        campaign.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var campaign = await context.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.DeletedAt == null, ct);

        if (campaign == null)
        {
            return false;
        }

        campaign.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> GetCampaignPreviewAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return "<p>Campaign not found</p>";
        }

        var baseUrl = _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://reservecloud.com";

        // Return the HTML content with placeholder values
        return campaign.HtmlContent
            .Replace("{{unsubscribe_url}}", $"{baseUrl}/newsletter/unsubscribe?id=preview")
            .Replace("{{subscriber_name}}", "Preview User")
            .Replace("{{subscriber_email}}", "preview@example.com");
    }

    public async Task<ScheduledCampaignProcessResult> ProcessScheduledCampaignsAsync(CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Find all scheduled campaigns that are due
        var dueCampaigns = await context.NewsletterCampaigns
            .Where(c => c.Status == CampaignStatus.Scheduled 
                     && c.ScheduledAt != null 
                     && c.ScheduledAt <= DateTime.UtcNow
                     && c.DeletedAt == null)
            .ToListAsync(ct);

        if (dueCampaigns.Count == 0)
        {
            return new ScheduledCampaignProcessResult(0, 0, 0);
        }

        int totalSent = 0;
        int totalFailed = 0;

        foreach (var campaign in dueCampaigns)
        {
            _logger.LogInformation("Processing scheduled campaign: {CampaignId} ({Name})", campaign.Id, campaign.Name);

            var result = await SendCampaignAsync(campaign.Id, ct);
            totalSent += result.SentCount;
            totalFailed += result.FailedCount;
        }

        return new ScheduledCampaignProcessResult(
            CampaignsProcessed: dueCampaigns.Count,
            TotalSent: totalSent,
            TotalFailed: totalFailed
        );
    }

    #endregion
}
