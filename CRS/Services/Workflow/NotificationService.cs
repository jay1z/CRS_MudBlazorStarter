using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using CRS.Data;
using CRS.Models.Workflow;

namespace CRS.Services.Workflow {
    /// <summary>
    /// Notification options for workflow automation intervals.
    /// </summary>
    public class WorkflowOptions {
        /// <summary>Hours until the first proposal unsigned reminder.</summary>
        public int ProposalReminder24hHours { get; set; } = 24;
        /// <summary>Hours until the second proposal unsigned reminder.</summary>
        public int ProposalReminder72hHours { get; set; } = 72;
        /// <summary>Days of inactivity before daily reminders start.</summary>
        public int ReminderDelayDays { get; set; } = 5;
        /// <summary>Years before scheduling renewal after archive.</summary>
        public int RenewalYears { get; set; } = 3;
    }

    /// <summary>
    /// Handles notifications and scheduling for workflow state changes.
    /// Replace stubbed implementations with email/SMS providers or background schedulers.
    /// </summary>
    public interface INotificationService {
        /// <summary>Send an immediate notification.</summary>
        Task SendAsync(string subject, string message, int tenantId, Guid? requestId = null);
        /// <summary>Send notification to specific user if they have notifications enabled.</summary>
        Task SendToUserAsync(string userId, string subject, string message, Guid? requestId = null);
        /// <summary>Schedule a reminder at a specified time.</summary>
        Task SendReminderAsync(string subject, string message, DateTime when, int tenantId, Guid? requestId = null);
        /// <summary>Schedule a renewal reminder.</summary>
        Task ScheduleRenewalAsync(DateTime when, int tenantId, Guid requestId);
        /// <summary>Handle automations for a state change.</summary>
        Task OnStateChangedAsync(StudyRequest request, StudyStatus oldState, StudyStatus newState);
        /// <summary>Check if a user has notifications enabled.</summary>
        Task<bool> IsUserNotificationsEnabledAsync(string userId);
    }

    public class NotificationService : INotificationService {
        private readonly WorkflowOptions _options;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IOptions<WorkflowOptions> options,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<NotificationService> logger) {
            _options = options.Value ?? new WorkflowOptions();
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<bool> IsUserNotificationsEnabledAsync(string userId) {
            if (string.IsNullOrEmpty(userId)) return false;
            
            try {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var user = await db.Users.FindAsync(Guid.Parse(userId));
                return user?.WorkflowNotificationsEnabled ?? true;
            } catch {
                return true; // Default to enabled if we can't check
            }
        }

        public async Task SendToUserAsync(string userId, string subject, string message, Guid? requestId = null) {
            if (!await IsUserNotificationsEnabledAsync(userId)) {
                _logger.LogDebug("Notification skipped for user {UserId} - notifications disabled", userId);
                return;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users.FindAsync(Guid.Parse(userId));
            if (user == null) return;

            // TODO: Integrate with your mail/SMS service here
            _logger.LogInformation(
                "[Notify][User:{UserId}][Email:{Email}] {Subject} :: {Message} (req:{RequestId})",
                userId, user.Email, subject, message, requestId);
        }

        public Task SendAsync(string subject, string message, int tenantId, Guid? requestId = null) {
            // Integrate with your mail/SMS service here
            _logger.LogInformation(
                "[Notify][Tenant:{TenantId}] {Subject} :: {Message} (req:{RequestId})",
                tenantId, subject, message, requestId);
            return Task.CompletedTask;
        }

        public Task SendReminderAsync(string subject, string message, DateTime when, int tenantId, Guid? requestId = null) {
            // Hook to a scheduler (Coravel Scheduler) when implementing real jobs
            _logger.LogInformation(
                "[Reminder scheduled @ {When:u}][Tenant:{TenantId}] {Subject} :: {Message} (req:{RequestId})",
                when, tenantId, subject, message, requestId);
            return Task.CompletedTask;
        }

        public Task ScheduleRenewalAsync(DateTime when, int tenantId, Guid requestId) {
            _logger.LogInformation(
                "[Renewal scheduled @ {When:u}][Tenant:{TenantId}] for request {RequestId}",
                when, tenantId, requestId);
            return Task.CompletedTask;
        }

        public async Task OnStateChangedAsync(StudyRequest request, StudyStatus oldState, StudyStatus newState) {
            var config = StageConfiguration.GetConfig(newState);
            
            await SendAsync(
                $"Status Update: {newState.ToDisplayTitle()}", 
                $"Request {request.Id} transitioned from {oldState.ToDisplayTitle()} to {newState.ToDisplayTitle()}.", 
                request.TenantId, 
                request.Id);

            // Send notifications to appropriate actors based on stage config
            await NotifyActorsAsync(request, config, newState);

            // Automations based on new state
            if (newState == StudyStatus.ProposalSent) {
                await SendReminderAsync(
                    "Proposal awaiting signature", 
                    "24h reminder", 
                    DateTime.UtcNow.AddHours(_options.ProposalReminder24hHours), 
                    request.TenantId, 
                    request.Id);
                await SendReminderAsync(
                    "Proposal awaiting signature", 
                    "72h reminder", 
                    DateTime.UtcNow.AddHours(_options.ProposalReminder72hHours), 
                    request.TenantId, 
                    request.Id);
            }

            if (newState == StudyStatus.FinancialInfoRequested || newState == StudyStatus.FinancialInfoInProgress) {
                // After N days, send daily reminders
                await SendReminderAsync(
                    $"Reminder: {newState.ToDisplayTitle()}", 
                    "Action required - please complete your financial information", 
                    DateTime.UtcNow.AddDays(_options.ReminderDelayDays), 
                    request.TenantId, 
                    request.Id);
            }

            if (newState == StudyStatus.RequestArchived) {
                await ScheduleRenewalAsync(
                    DateTime.UtcNow.AddYears(_options.RenewalYears), 
                    request.TenantId, 
                    request.Id);
            }
        }

        private async Task NotifyActorsAsync(StudyRequest request, StageConfig config, StudyStatus newState) {
            if (config.NotifyOnEnter == StageActor.None) return;

            try {
                await using var db = await _dbFactory.CreateDbContextAsync();
                
                // Get the reserve study for more context
                var study = await db.ReserveStudies
                    .Include(s => s.Community)
                    .Include(s => s.Specialist)
                    .Include(s => s.Contact)
                    .FirstOrDefaultAsync(s => s.Id == request.Id);

                if (study == null) return;

                var communityName = study.Community?.Name ?? "Your Community";
                var subject = $"Action Required: {newState.ToDisplayTitle()} - {communityName}";
                var message = config.NotifyOnEnter.HasFlag(StageActor.HOA) 
                    ? config.WaitingMessage 
                    : config.ActionRequiredMessage;

                // Notify HOA contact
                if (config.NotifyOnEnter.HasFlag(StageActor.HOA) && study.Contact != null) {
                    _logger.LogInformation(
                        "[Notify HOA][Contact:{Email}] {Subject}",
                        study.Contact.Email, subject);
                    // TODO: Send email to study.Contact.Email
                }

                // Notify Specialist
                if (config.NotifyOnEnter.HasFlag(StageActor.Specialist) && study.Specialist != null) {
                    await SendToUserAsync(study.Specialist.Id.ToString(), subject, message, request.Id);
                }

                // Notify Staff - get all TenantOwner users for this tenant
                if (config.NotifyOnEnter.HasFlag(StageActor.Staff)) {
                    var staffUsers = await db.Users
                        .Where(u => u.TenantId == request.TenantId)
                        .ToListAsync();
                    
                    foreach (var staffUser in staffUsers.Where(u => u.Roles?.Contains("TenantOwner") == true)) {
                        await SendToUserAsync(staffUser.Id.ToString(), subject, message, request.Id);
                    }
                }

                // Notify Admins
                if (config.NotifyOnEnter.HasFlag(StageActor.Admin)) {
                    var adminUsers = await db.Users
                        .Where(u => u.Roles != null && u.Roles.Contains("PlatformAdmin"))
                        .ToListAsync();
                    
                    foreach (var admin in adminUsers) {
                        await SendToUserAsync(admin.Id.ToString(), subject, message, request.Id);
                    }
                }
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to send actor notifications for request {RequestId}", request.Id);
            }
        }
    }
}
