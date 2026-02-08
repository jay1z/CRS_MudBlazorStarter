using Coravel.Invocable;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRS.Jobs;

/// <summary>
/// Scheduled job to send automated invoice reminders.
/// Sends reminders at configurable intervals before and after due date.
/// Runs daily via Coravel scheduler.
/// </summary>
public class InvoiceReminderInvocable : IInvocable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IMailer _mailer;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceReminderInvocable> _logger;
    private readonly IConfiguration _configuration;

    // Default reminder schedule: days relative to due date (negative = before, positive = after)
    // Used as fallback when tenant doesn't specify ReminderFrequencyDays
    private static readonly int[] DefaultReminderDays = { -3, 1, 7, 14, 30 };

    public InvoiceReminderInvocable(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailer mailer,
        IInvoiceService invoiceService,
        ILogger<InvoiceReminderInvocable> logger,
        IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _mailer = mailer;
        _invoiceService = invoiceService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Starting automated invoice reminder job at {Time}", DateTime.UtcNow);

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Get all tenants with their reminder settings including frequency
            var tenants = await context.Tenants
                .Where(t => t.ProvisioningStatus == TenantProvisioningStatus.Active)
                .ToDictionaryAsync(t => t.Id, t => new { 
                    t.SendAutomaticReminders, 
                    t.AutoSendInvoiceReminders,
                    t.ReminderFrequencyDays 
                });

            // Get all unpaid, non-voided invoices that have been sent
            var invoices = await context.Invoices
                .Include(i => i.ReserveStudy)
                    .ThenInclude(rs => rs!.Community)
                .Where(i => i.DateDeleted == null &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Voided &&
                            i.Status != InvoiceStatus.Draft &&
                            !string.IsNullOrEmpty(i.BillToEmail))
                .ToListAsync();

            _logger.LogInformation("Found {Count} invoices to check for reminders", invoices.Count);

            var remindersSet = 0;
            var skippedByTenantSetting = 0;
            var today = DateTime.UtcNow.Date;

            foreach (var invoice in invoices)
            {
                try
                {
                    // Get tenant settings with defaults
                    var reminderFrequency = 7; // Default to weekly
                    
                    if (tenants.TryGetValue(invoice.TenantId, out var tenantSettings))
                    {
                        // Skip if tenant has automatic reminders disabled
                        if (!tenantSettings.SendAutomaticReminders || !tenantSettings.AutoSendInvoiceReminders)
                        {
                            skippedByTenantSetting++;
                            continue;
                        }
                        
                        reminderFrequency = tenantSettings.ReminderFrequencyDays;
                    }

                    if (ShouldSendReminder(invoice, today, reminderFrequency))
                    {
                        await SendReminderAsync(invoice);
                        remindersSet++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process reminder for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                }
            }

            _logger.LogInformation(
                "Invoice reminder job completed. Sent {Count} reminders. Skipped {Skipped} (tenant settings disabled).", 
                remindersSet, 
                skippedByTenantSetting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running invoice reminder job");
            throw;
        }
    }

    private bool ShouldSendReminder(Invoice invoice, DateTime today, int reminderFrequencyDays)
    {
        var daysFromDue = (today - invoice.DueDate).Days;

        // Check if we already sent a reminder today
        if (invoice.LastReminderSent.HasValue &&
            invoice.LastReminderSent.Value.Date == today)
        {
            return false; // Already sent today
        }

        // Don't send too many reminders (max 10)
        if (invoice.ReminderCount >= 10)
        {
            return false;
        }

        // Always send reminder 3 days before due
        if (daysFromDue == -3)
        {
            return true;
        }

        // For overdue invoices, use tenant's ReminderFrequencyDays setting
        if (daysFromDue > 0 && reminderFrequencyDays > 0)
        {
            // Send reminder if days overdue is divisible by frequency
            // E.g., if frequency is 7, send on days 7, 14, 21, 28...
            if (daysFromDue % reminderFrequencyDays == 0)
            {
                return true;
            }
        }
        else if (daysFromDue > 0)
        {
            // Fallback to default schedule if no tenant frequency set
            return DefaultReminderDays.Contains(daysFromDue);
        }

        return false;
    }

    private async Task SendReminderAsync(Invoice invoice)
    {
        var daysOverdue = (DateTime.UtcNow.Date - invoice.DueDate).Days;
        var isOverdue = daysOverdue > 0;

        _logger.LogInformation(
            "Sending reminder for invoice {InvoiceNumber} ({DaysOverdue} days {Status})",
            invoice.InvoiceNumber,
            Math.Abs(daysOverdue),
            isOverdue ? "overdue" : "until due");

        // Get base URL from configuration
        var baseUrl = _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://app.reservecloud.com";

        // Create email model
        var emailModel = new InvoiceEmail
        {
            Invoice = invoice,
            ReserveStudy = invoice.ReserveStudy,
            BaseUrl = baseUrl,
            InvoiceViewUrl = $"{baseUrl}/Invoices/Details/{invoice.Id}",
            IsReminder = true,
            DaysPastDue = isOverdue ? daysOverdue : 0
        };

        // Send email
        var mailable = new InvoiceMailable(emailModel, invoice.BillToEmail!);
        await _mailer.SendAsync(mailable);

        // Update reminder tracking
        await _invoiceService.RecordReminderSentAsync(invoice.Id);

        _logger.LogInformation(
            "Sent reminder #{Count} to {Email} for invoice {InvoiceNumber}",
            invoice.ReminderCount + 1,
            invoice.BillToEmail,
            invoice.InvoiceNumber);
    }
}
