using Coravel.Invocable;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email.Mailables;
using Microsoft.EntityFrameworkCore;

namespace CRS.Jobs;

/// <summary>
/// Scheduled job to handle trial expiration notifications and status updates.
/// Runs daily via Coravel scheduler to:
/// - Send warning emails 7 days before trial ends
/// - Send warning emails 3 days before trial ends
/// - Send urgent emails 1 day before trial ends
/// - Suspend access when trial expires
/// </summary>
public class TrialExpirationInvocable : IInvocable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IMailer _mailer;
    private readonly ILogger<TrialExpirationInvocable> _logger;

    public TrialExpirationInvocable(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailer mailer,
        ILogger<TrialExpirationInvocable> logger)
    {
        _dbFactory = dbFactory;
        _mailer = mailer;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Starting trial expiration check job at {Time}", DateTime.UtcNow);

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.UtcNow.Date;

            // Get all tenants currently in trial
            var trialingTenants = await context.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Trialing &&
                           t.TrialExpiresAt.HasValue &&
                           t.ProvisioningStatus == TenantProvisioningStatus.Active)
                .ToListAsync();

            _logger.LogInformation("Found {Count} tenants in trial status", trialingTenants.Count);

            var warningsSent = 0;
            var urgentSent = 0;
            var expired = 0;

            foreach (var tenant in trialingTenants)
            {
                var daysRemaining = (tenant.TrialExpiresAt!.Value.Date - today).Days;

                try
                {
                    if (daysRemaining <= 0)
                    {
                        // Trial has expired - suspend access
                        await ExpireTrialAsync(context, tenant);
                        expired++;
                    }
                    else if (daysRemaining == 1)
                    {
                        // 1 day left - send urgent email
                        await SendTrialEndingEmailAsync(tenant, daysRemaining, isUrgent: true);
                        urgentSent++;
                    }
                    else if (daysRemaining == 3)
                    {
                        // 3 days left - send warning email
                        await SendTrialEndingEmailAsync(tenant, daysRemaining, isUrgent: false);
                        warningsSent++;
                    }
                    else if (daysRemaining == 7)
                    {
                        // 7 days left - send early warning
                        await SendTrialEndingEmailAsync(tenant, daysRemaining, isUrgent: false);
                        warningsSent++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing trial for tenant {TenantId} ({TenantName})", 
                        tenant.Id, tenant.Name);
                }
            }

            _logger.LogInformation(
                "Trial expiration job completed. Warnings sent: {Warnings}, Urgent sent: {Urgent}, Expired: {Expired}",
                warningsSent, urgentSent, expired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running trial expiration job");
            throw;
        }
    }

    /// <summary>
    /// Sends a trial ending notification email to the tenant owner.
    /// </summary>
    private async Task SendTrialEndingEmailAsync(Tenant tenant, int daysRemaining, bool isUrgent)
    {
        var ownerEmail = tenant.PendingOwnerEmail ?? tenant.DefaultNotificationEmail;

        if (string.IsNullOrEmpty(ownerEmail))
        {
            _logger.LogWarning("Cannot send trial ending email to tenant {TenantId} - no email address", tenant.Id);
            return;
        }

        try
        {
            var emailModel = new BillingNotificationEmail
            {
                NotificationType = BillingNotificationType.TrialEnding,
                TenantName = tenant.Name,
                OwnerEmail = ownerEmail,
                OwnerName = null, // Will be populated if we have owner info
                GracePeriodEndsAt = tenant.TrialExpiresAt,
                BillingPortalUrl = $"https://{tenant.Subdomain}.alxreservecloud.com/account/billing"
            };

            var mailable = new TrialEndingMailable(emailModel, daysRemaining, isUrgent);
            await _mailer.SendAsync(mailable);

            _logger.LogInformation(
                "Sent trial ending email to {Email} for tenant {TenantId} ({DaysRemaining} days remaining)",
                ownerEmail, tenant.Id, daysRemaining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send trial ending email to tenant {TenantId}", tenant.Id);
        }
    }

    /// <summary>
    /// Expires a trial and suspends tenant access.
    /// </summary>
    private async Task ExpireTrialAsync(ApplicationDbContext context, Tenant tenant)
    {
        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.GracePeriodEndsAt = DateTime.UtcNow.AddDays(7); // 7 day grace period to convert
        tenant.UpdatedAt = DateTime.UtcNow;

        context.Tenants.Update(tenant);
        await context.SaveChangesAsync();

        // Send expiration notification
        var ownerEmail = tenant.PendingOwnerEmail ?? tenant.DefaultNotificationEmail;
        if (!string.IsNullOrEmpty(ownerEmail))
        {
            try
            {
                var emailModel = new BillingNotificationEmail
                {
                    NotificationType = BillingNotificationType.AccountSuspended,
                    TenantName = tenant.Name,
                    OwnerEmail = ownerEmail,
                    SuspendedAt = DateTime.UtcNow,
                    GracePeriodEndsAt = tenant.GracePeriodEndsAt,
                    BillingPortalUrl = $"https://{tenant.Subdomain}.alxreservecloud.com/account/billing"
                };

                var mailable = new TrialExpiredMailable(emailModel);
                await _mailer.SendAsync(mailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send trial expired email to tenant {TenantId}", tenant.Id);
            }
        }

        _logger.LogInformation("Trial expired for tenant {TenantId} ({TenantName}). Grace period until {GracePeriodEnd}",
            tenant.Id, tenant.Name, tenant.GracePeriodEndsAt);
    }
}
