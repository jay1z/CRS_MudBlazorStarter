using Coravel.Invocable;
using CRS.Services.Interfaces;

namespace CRS.Jobs;

/// <summary>
/// Scheduled job to calculate and apply late payment interest to overdue invoices.
/// Runs daily via Coravel scheduler.
/// </summary>
public class LateInterestInvocable : IInvocable
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<LateInterestInvocable> _logger;

    public LateInterestInvocable(
        IInvoiceService invoiceService,
        ILogger<LateInterestInvocable> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Starting late interest calculation job at {Time}", DateTime.UtcNow);

        try
        {
            // First, update any invoices that are now overdue
            await _invoiceService.UpdateOverdueStatusesAsync();
            _logger.LogInformation("Updated overdue statuses");

            // Then calculate and apply late interest
            var invoicesUpdated = await _invoiceService.CalculateLateInterestAsync();
            _logger.LogInformation("Applied late interest to {Count} invoices", invoicesUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running late interest calculation job");
            throw;
        }
    }
}
