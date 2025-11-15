using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using CRS.Models.Workflow;

namespace CRS.Services.Workflow {
    /// <summary>
    /// Notification options for workflow automation intervals.
    /// </summary>
    public class WorkflowOptions {
        /// <summary>Hours until the first proposal unsigned reminder.</summary>
        public int ProposalReminder24hHours { get; set; } =24;
        /// <summary>Hours until the second proposal unsigned reminder.</summary>
        public int ProposalReminder72hHours { get; set; } =72;
        /// <summary>Days of inactivity before daily reminders start.</summary>
        public int ReminderDelayDays { get; set; } =5;
        /// <summary>Years before scheduling renewal after archive.</summary>
        public int RenewalYears { get; set; } =3;
    }

    /// <summary>
    /// Handles notifications and scheduling for workflow state changes.
    /// Replace stubbed implementations with email/SMS providers or background schedulers.
    /// </summary>
    public interface INotificationService {
        /// <summary>Send an immediate notification.</summary>
        Task SendAsync(string subject, string message, int tenantId, Guid? requestId = null);
        /// <summary>Schedule a reminder at a specified time.</summary>
        Task SendReminderAsync(string subject, string message, DateTime when, int tenantId, Guid? requestId = null);
        /// <summary>Schedule a renewal reminder.</summary>
        Task ScheduleRenewalAsync(DateTime when, int tenantId, Guid requestId);
        /// <summary>Handle automations for a state change.</summary>
        Task OnStateChangedAsync(StudyRequest request, StudyStatus oldState, StudyStatus newState);
    }

    public class NotificationService : INotificationService {
        private readonly WorkflowOptions _options;
        public NotificationService(IOptions<WorkflowOptions> options) {
            _options = options.Value ?? new WorkflowOptions();
        }

        public Task SendAsync(string subject, string message, int tenantId, Guid? requestId = null) {
            // Integrate with your mail/SMS service here
            Console.WriteLine($"[Notify][Tenant:{tenantId}] {subject} :: {message} (req:{requestId})");
            return Task.CompletedTask;
        }

        public Task SendReminderAsync(string subject, string message, DateTime when, int tenantId, Guid? requestId = null) {
            // Hook to a scheduler (Coravel Scheduler) when implementing real jobs
            Console.WriteLine($"[Reminder scheduled @ {when:u}][Tenant:{tenantId}] {subject} :: {message} (req:{requestId})");
            return Task.CompletedTask;
        }

        public Task ScheduleRenewalAsync(DateTime when, int tenantId, Guid requestId) {
            Console.WriteLine($"[Renewal scheduled @ {when:u}][Tenant:{tenantId}] for request {requestId}");
            return Task.CompletedTask;
        }

        public async Task OnStateChangedAsync(StudyRequest request, StudyStatus oldState, StudyStatus newState) {
            await SendAsync($"State changed: {oldState} -> {newState}", $"Request {request.Id} transitioned.", request.TenantId, request.Id);

            // Automations
            if (newState == StudyStatus.ProposalPendingESign) {
                await SendReminderAsync("Proposal awaiting signature", "24h reminder", DateTime.UtcNow.AddHours(_options.ProposalReminder24hHours), request.TenantId, request.Id);
                await SendReminderAsync("Proposal awaiting signature", "72h reminder", DateTime.UtcNow.AddHours(_options.ProposalReminder72hHours), request.TenantId, request.Id);
            }

            if (newState == StudyStatus.NeedsInfo || newState == StudyStatus.UnderReview) {
                // After N days, send daily reminders (scheduling logic will repeat in scheduler)
                await SendReminderAsync($"Reminder: {newState}", "Action required", DateTime.UtcNow.AddDays(_options.ReminderDelayDays), request.TenantId, request.Id);
            }

            if (newState == StudyStatus.Archived) {
                await ScheduleRenewalAsync(DateTime.UtcNow.AddYears(_options.RenewalYears), request.TenantId, request.Id);
            }
        }
    }
}
