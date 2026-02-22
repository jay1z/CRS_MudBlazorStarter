using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Coravel.Invocable;
using Coravel.Mailer.Mail.Interfaces;

namespace CRS.Jobs {
    /// <summary>
    /// Background job that manages tenant subscription lifecycle transitions.
    /// Handles: PastDue → GracePeriod → Suspended → MarkedForDeletion → Deleted
    /// Should be scheduled to run daily via Coravel or similar scheduler.
    /// </summary>
    public class TenantLifecycleJob : IInvocable {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<TenantLifecycleJob> _logger;
        private readonly IMailer _mailer;
        private readonly ITenantArchiveService _archiveService;

        public TenantLifecycleJob(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<TenantLifecycleJob> logger,
            IMailer mailer,
            ITenantArchiveService archiveService) {
            _dbFactory = dbFactory;
            _logger = logger;
            _mailer = mailer;
            _archiveService = archiveService;
        }

        /// <summary>
        /// Main entry point - processes all lifecycle transitions
        /// Required by IInvocable interface
        /// </summary>
        public async Task Invoke() {
            await ExecuteAsync();
        }
        
        /// <summary>
        /// Main entry point - processes all lifecycle transitions
        /// </summary>
        public async Task ExecuteAsync() {
            _logger.LogInformation("Starting tenant lifecycle job");
            
            try {
                await ProcessPastDueToGracePeriodAsync();
                await ProcessGracePeriodToSuspendedAsync();
                await ProcessSuspendedToMarkedForDeletionAsync();
                await ProcessMarkedForDeletionAsync();
                
                _logger.LogInformation("Tenant lifecycle job completed successfully");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error processing tenant lifecycle job");
                throw;
            }
        }

        /// <summary>
        /// Move tenants from PastDue to GracePeriod after 7 days
        /// PastDue = Stripe is retrying payments, full access continues
        /// GracePeriod = Stripe retries exhausted, read-only access begins
        /// </summary>
        private async Task ProcessPastDueToGracePeriodAsync() {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-7); // 7 days ago

            var tenants = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.PastDue)
                .Where(t => t.LastPaymentFailureAt != null && t.LastPaymentFailureAt < cutoff)
                .ToListAsync();

            _logger.LogInformation("Processing {Count} tenants from PastDue to GracePeriod", tenants.Count);

            foreach (var tenant in tenants) {
                tenant.SubscriptionStatus = SubscriptionStatus.GracePeriod;
                tenant.GracePeriodEndsAt = now.AddDays(30); // 30 days of read-only access
                tenant.UpdatedAt = now;
                
                _logger.LogInformation("Tenant {TenantId} ({Name}) entered GracePeriod. Read-only access until {EndsAt}", 
                    tenant.Id, tenant.Name, tenant.GracePeriodEndsAt);

                // Send grace period notification email
                await SendBillingNotificationAsync(db, tenant, BillingNotificationType.GracePeriodStarted);
            }

            if (tenants.Any()) {
                await db.SaveChangesAsync();
                _logger.LogInformation("Transitioned {Count} tenants to GracePeriod", tenants.Count);
            }
        }

        /// <summary>
        /// Move tenants from GracePeriod to Suspended after grace period expires
        /// GracePeriod = Read-only access
        /// Suspended = No access, but data fully preserved
        /// </summary>
        private async Task ProcessGracePeriodToSuspendedAsync() {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            var tenants = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.GracePeriod)
                .Where(t => t.GracePeriodEndsAt != null && t.GracePeriodEndsAt < now)
                .ToListAsync();

            _logger.LogInformation("Processing {Count} tenants from GracePeriod to Suspended", tenants.Count);

            foreach (var tenant in tenants) {
                tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
                tenant.SuspendedAt = now;
                tenant.IsActive = false; // Remove application access
                tenant.UpdatedAt = now;
                
                _logger.LogInformation("Tenant {TenantId} ({Name}) suspended. Data will be retained for 90 days.", 
                    tenant.Id, tenant.Name);

                // Send suspension notification email
                await SendBillingNotificationAsync(db, tenant, BillingNotificationType.AccountSuspended);
            }

            if (tenants.Any()) {
                await db.SaveChangesAsync();
                _logger.LogInformation("Suspended {Count} tenants", tenants.Count);
            }
        }

        /// <summary>
        /// Move tenants from Suspended to MarkedForDeletion after 90 days
        /// Suspended = No access, data preserved
        /// MarkedForDeletion = Final warning, deletion scheduled
        /// </summary>
        private async Task ProcessSuspendedToMarkedForDeletionAsync() {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-90); // 90 days ago

            var tenants = await db.Tenants
                .Where(t => t.SubscriptionStatus == SubscriptionStatus.Suspended)
                .Where(t => t.SuspendedAt != null && t.SuspendedAt < cutoff)
                .Where(t => !t.IsMarkedForDeletion) // Ensure not already marked
                .ToListAsync();

            _logger.LogInformation("Processing {Count} tenants from Suspended to MarkedForDeletion", tenants.Count);

            foreach (var tenant in tenants) {
                tenant.SubscriptionStatus = SubscriptionStatus.MarkedForDeletion;
                tenant.IsMarkedForDeletion = true;
                tenant.DeletionReason = "payment_failed";
                // Schedule permanent deletion for 275 days from now (365 days total from suspension)
                tenant.DeletionScheduledAt = now.AddDays(275);
                tenant.UpdatedAt = now;
                
                _logger.LogWarning("Tenant {TenantId} ({Name}) marked for deletion. Data will be permanently deleted on {DeletionDate}", 
                    tenant.Id, tenant.Name, tenant.DeletionScheduledAt);

                // Send deletion warning email (reuses suspension template with deletion date)
                await SendBillingNotificationAsync(db, tenant, BillingNotificationType.AccountSuspended);
            }

            if (tenants.Any()) {
                await db.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} tenants for deletion", tenants.Count);
            }
        }

        /// <summary>
        /// Send reminders for tenants marked for deletion and permanently delete when scheduled date arrives
        /// </summary>
        private async Task ProcessMarkedForDeletionAsync() {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            // Send reminders at 30 days before deletion
            var reminders = await db.Tenants
                .Where(t => t.IsMarkedForDeletion)
                .Where(t => t.DeletionScheduledAt != null)
                .Where(t => t.DeletionScheduledAt <= now.AddDays(30))
                .Where(t => t.DeletionScheduledAt > now) // Not yet deleted
                .ToListAsync();

            foreach (var tenant in reminders) {
                var daysUntilDeletion = (tenant.DeletionScheduledAt!.Value - now).Days;
                _logger.LogInformation("Tenant {TenantId} ({Name}) will be deleted in {Days} days", 
                    tenant.Id, tenant.Name, daysUntilDeletion);

                // Send final deletion reminder emails at specific intervals (30, 14, 7, 3, 1 days before)
                if (daysUntilDeletion == 30 || daysUntilDeletion == 14 || daysUntilDeletion == 7 || 
                    daysUntilDeletion == 3 || daysUntilDeletion == 1)
                {
                    await SendBillingNotificationAsync(db, tenant, BillingNotificationType.AccountSuspended);
                }
            }

            // Permanently delete tenants whose deletion date has arrived
            var toDelete = await db.Tenants
                .Where(t => t.IsMarkedForDeletion)
                .Where(t => t.DeletionScheduledAt != null && t.DeletionScheduledAt < now)
                .ToListAsync();

            _logger.LogInformation("Processing {Count} tenants for permanent deletion", toDelete.Count);

            foreach (var tenant in toDelete) {
                await PermanentlyDeleteTenantAsync(db, tenant);
            }

            if (toDelete.Any()) {
                await db.SaveChangesAsync();
                _logger.LogInformation("Permanently deleted {Count} tenants", toDelete.Count);
            }
        }

        /// <summary>
        /// Permanently delete a tenant and all associated data
        /// </summary>
        private async Task PermanentlyDeleteTenantAsync(ApplicationDbContext db, Tenant tenant) {
            _logger.LogWarning("Permanently deleting tenant {TenantId} ({Name}) - IRREVERSIBLE", 
                tenant.Id, tenant.Name);

            try {
                // 1. Archive financial records to cold storage (legal requirement for accounting/tax purposes)
                var archiveResult = await _archiveService.ArchiveTenantDataAsync(
                    tenant.Id, 
                    tenant.DeletionReason ?? "permanent_deletion");

                if (archiveResult.Success)
                {
                    _logger.LogInformation(
                        "Archived tenant {TenantId} data: {Invoices} invoices, {Payments} payments, {Reports} reports",
                        tenant.Id, archiveResult.InvoicesArchived, archiveResult.PaymentsArchived, archiveResult.ReportsArchived);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to archive tenant {TenantId} data: {Error}. Proceeding with deletion anyway.",
                        tenant.Id, archiveResult.ErrorMessage);
                }

                // 2. Delete all tenant-scoped data
                // Note: This is a simplified version. In production, you should:
                // - Delete in correct order to respect foreign keys
                // - Use bulk delete operations for performance
                // - Consider archiving instead of deleting for audit trail

                // Delete communities and related data
                var communities = await db.Communities.Where(c => c.TenantId == tenant.Id).ToListAsync();
                db.Communities.RemoveRange(communities);

                // Delete reserve studies
                var studies = await db.ReserveStudies.Where(r => r.TenantId == tenant.Id).ToListAsync();
                db.ReserveStudies.RemoveRange(studies);

                // Delete contacts
                var contacts = await db.Contacts.Where(c => c.TenantId == tenant.Id).ToListAsync();
                db.Contacts.RemoveRange(contacts);
                
                // Delete property managers
                var propertyManagers = await db.PropertyManagers.Where(p => p.TenantId == tenant.Id).ToListAsync();
                db.PropertyManagers.RemoveRange(propertyManagers);
                
                // Delete messages
                var messages = await db.Messages.Where(m => m.TenantId == tenant.Id).ToListAsync();
                db.Messages.RemoveRange(messages);
                
                // Delete users (tenant-scoped)
                var users = await db.Users.Where(u => u.TenantId == tenant.Id).ToListAsync();
                foreach (var user in users) {
                    // Delete user role assignments
                    var roleAssignments = await db.UserRoleAssignments.Where(ura => ura.UserId == user.Id).ToListAsync();
                    db.UserRoleAssignments.RemoveRange(roleAssignments);
                }
                db.Users.RemoveRange(users);

                // 3. Soft delete the tenant (keep for audit trail)
                var ownerEmail = tenant.PendingOwnerEmail; // Save before modification
                var tenantName = tenant.Name; // Save original name

                tenant.DateDeleted = DateTime.UtcNow;
                tenant.IsActive = false;
                tenant.SubscriptionStatus = SubscriptionStatus.Canceled;
                tenant.Name = $"[DELETED] {tenant.Name}"; // Mark in name for clarity
                tenant.Subdomain = $"deleted-{tenant.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}"; // Free up subdomain

                _logger.LogInformation("Tenant {TenantId} data deleted successfully", tenant.Id);

                // Send final deletion confirmation email to archived email address
                if (!string.IsNullOrEmpty(ownerEmail))
                {
                    try
                    {
                        var email = new BillingNotificationEmail
                        {
                            NotificationType = BillingNotificationType.AccountSuspended, // Closest match
                            TenantName = tenantName,
                            OwnerEmail = ownerEmail,
                            SuspendedAt = DateTime.UtcNow
                        };
                        var mailable = new BillingNotificationMailable(email);
                        await _mailer.SendAsync(mailable);
                        _logger.LogInformation("Sent deletion confirmation email to {Email} for tenant {TenantId}", ownerEmail, tenant.Id);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send deletion confirmation email for tenant {TenantId}", tenant.Id);
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Error deleting tenant {TenantId} data", tenant.Id);
                // Don't throw - log and continue with other tenants
            }
        }

        /// <summary>
        /// Get summary statistics for monitoring
        /// </summary>
        public async Task<LifecycleStatistics> GetStatisticsAsync() {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var stats = new LifecycleStatistics {
                ActiveTenants = await db.Tenants.CountAsync(t => t.SubscriptionStatus == SubscriptionStatus.Active),
                PastDueTenants = await db.Tenants.CountAsync(t => t.SubscriptionStatus == SubscriptionStatus.PastDue),
                GracePeriodTenants = await db.Tenants.CountAsync(t => t.SubscriptionStatus == SubscriptionStatus.GracePeriod),
                SuspendedTenants = await db.Tenants.CountAsync(t => t.SubscriptionStatus == SubscriptionStatus.Suspended),
                MarkedForDeletionTenants = await db.Tenants.CountAsync(t => t.IsMarkedForDeletion),
                TotalTenants = await db.Tenants.CountAsync(t => !t.DateDeleted.HasValue)
            };

            return stats;
        }

        /// <summary>
        /// Sends a billing notification email to the tenant owner.
        /// </summary>
        private async Task SendBillingNotificationAsync(
            ApplicationDbContext db,
            Tenant tenant, 
            BillingNotificationType notificationType)
        {
            try
            {
                // Get owner email - prefer OwnerId lookup, fallback to PendingOwnerEmail
                string? ownerEmail = null;
                string? ownerName = null;

                if (!string.IsNullOrEmpty(tenant.OwnerId) && Guid.TryParse(tenant.OwnerId, out var ownerId))
                {
                    var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
                    if (owner != null)
                    {
                        ownerEmail = owner.Email;
                        ownerName = owner.FullName;
                    }
                }

                ownerEmail ??= tenant.PendingOwnerEmail;

                if (string.IsNullOrEmpty(ownerEmail))
                {
                    _logger.LogWarning("Cannot send billing notification for tenant {TenantId}: no owner email found", tenant.Id);
                    return;
                }

                // Build URLs
                var billingPortalUrl = $"https://{tenant.Subdomain}.reservecloud.com/account/billing";
                var dashboardUrl = $"https://{tenant.Subdomain}.reservecloud.com";

                var email = new BillingNotificationEmail
                {
                    NotificationType = notificationType,
                    TenantName = tenant.Name,
                    OwnerEmail = ownerEmail,
                    OwnerName = ownerName,
                    PlanName = tenant.Tier?.ToString(),
                    GracePeriodEndsAt = tenant.GracePeriodEndsAt,
                    SuspendedAt = tenant.SuspendedAt,
                    ReactivatedAt = tenant.LastReactivatedAt,
                    ReactivationCount = tenant.ReactivationCount,
                    UpdatePaymentUrl = billingPortalUrl,
                    BillingPortalUrl = billingPortalUrl,
                    DashboardUrl = dashboardUrl
                };

                var mailable = new BillingNotificationMailable(email);
                await _mailer.SendAsync(mailable);

                _logger.LogInformation(
                    "Sent {NotificationType} email to {Email} for tenant {TenantId}",
                    notificationType, ownerEmail, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send {NotificationType} email for tenant {TenantId}", 
                    notificationType, tenant.Id);
                // Don't rethrow - notification failure shouldn't break lifecycle job
            }
        }
    }

    public class LifecycleStatistics {
        public int ActiveTenants { get; set; }
        public int PastDueTenants { get; set; }
        public int GracePeriodTenants { get; set; }
        public int SuspendedTenants { get; set; }
        public int MarkedForDeletionTenants { get; set; }
        public int TotalTenants { get; set; }
    }
}
