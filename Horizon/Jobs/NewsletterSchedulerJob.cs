using Horizon.Services.Interfaces;
using Coravel.Invocable;

namespace Horizon.Jobs;

/// <summary>
/// Background job that processes scheduled newsletter campaigns.
/// Should be scheduled to run every minute or every 5 minutes.
/// </summary>
public class NewsletterSchedulerJob : IInvocable
{
    private readonly INewsletterService _newsletterService;
    private readonly ILogger<NewsletterSchedulerJob> _logger;

    public NewsletterSchedulerJob(
        INewsletterService newsletterService,
        ILogger<NewsletterSchedulerJob> logger)
    {
        _newsletterService = newsletterService;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogDebug("Checking for scheduled newsletter campaigns...");

        try
        {
            var result = await _newsletterService.ProcessScheduledCampaignsAsync();
            
            if (result.CampaignsProcessed > 0)
            {
                _logger.LogInformation(
                    "Processed {Count} scheduled campaign(s): {Sent} sent, {Failed} failed",
                    result.CampaignsProcessed, result.TotalSent, result.TotalFailed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled newsletter campaigns");
        }
    }
}

/// <summary>
/// Result of processing scheduled campaigns.
/// </summary>
public record ScheduledCampaignProcessResult(
    int CampaignsProcessed,
    int TotalSent,
    int TotalFailed
);
