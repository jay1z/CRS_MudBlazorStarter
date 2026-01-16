using Coravel.Invocable;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    // Reminder schedule: days relative to due date (negative = before, positive = after)
    private static readonly int[] ReminderDays = { -3, 1, 7, 14, 30 };

    public InvoiceReminderInvocable(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailer mailer,
        IInvoiceService invoiceService,
        ILogger<InvoiceReminderInvocable> logger)
    {
        _dbFactory = dbFactory;
        _mailer = mailer;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Starting automated invoice reminder job at {Time}", DateTime.UtcNow);

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

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
            var today = DateTime.UtcNow.Date;

            foreach (var invoice in invoices)
            {
                try
                {
                    if (ShouldSendReminder(invoice, today))
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

            _logger.LogInformation("Invoice reminder job completed. Sent {Count} reminders.", remindersSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running invoice reminder job");
            throw;
        }
    }

    private bool ShouldSendReminder(Invoice invoice, DateTime today)
    {
        var daysFromDue = (today - invoice.DueDate).Days;

        // Check if today matches any of our reminder days
        foreach (var reminderDay in ReminderDays)
        {
            if (daysFromDue == reminderDay)
            {
                // Check if we already sent a reminder today
                if (invoice.LastReminderSent.HasValue &&
                    invoice.LastReminderSent.Value.Date == today)
                {
                    return false; // Already sent today
                }

                // Don't send too many reminders (max 5)
                if (invoice.ReminderCount >= 5)
                {
                    return false;
                }

                return true;
            }
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

        // Create email model
        var emailModel = new InvoiceEmail
        {
            Invoice = invoice,
            ReserveStudy = invoice.ReserveStudy,
            BaseUrl = "https://app.reservecloud.com", // TODO: Get from configuration
            InvoiceViewUrl = $"https://app.reservecloud.com/Invoices/{invoice.Id}",
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
