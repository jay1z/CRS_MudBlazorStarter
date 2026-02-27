using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Emails;
using Horizon.Services.Email;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Horizon.EventsAndListeners {
    public class FinancialInfoSubmittedListener : IListener<FinancialInfoSubmittedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<FinancialInfoSubmittedListener> _logger;

        public FinancialInfoSubmittedListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<FinancialInfoSubmittedListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(FinancialInfoSubmittedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;

            // Reload study with all needed navigation properties
            await using var db = await _dbFactory.CreateDbContextAsync();
            study = await db.ReserveStudies
                .Include(s => s.User)
                .Include(s => s.Specialist)
                .Include(s => s.Community)
                .Include(s => s.Contact)
                .FirstOrDefaultAsync(s => s.Id == study.Id);

            if (study == null)
            {
                _logger.LogWarning("Could not find study {StudyId} for financial info submitted email", broadcasted.ReserveStudy.Id);
                return;
            }

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, 
                "Financial documents have been submitted for review. Our team will process them and proceed with scheduling your site visit.");

            // Build subject with tenant company name
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Financial Information Received - {study.Community?.Name}";

            var recipients = new List<string>();

            // Notify the specialist
            if (!string.IsNullOrEmpty(study.Specialist?.Email)) {
                recipients.Add(study.Specialist.Email);
            }

            // Also notify the study owner as confirmation
            if (!string.IsNullOrEmpty(study.User?.Email) && 
                !recipients.Contains(study.User.Email, StringComparer.OrdinalIgnoreCase)) {
                recipients.Add(study.User.Email);
            }

            // Fallback to tenant's default notification email
            if (recipients.Count == 0 && !string.IsNullOrEmpty(email.TenantInfo?.DefaultNotificationEmail)) {
                recipients.Add(email.TenantInfo.DefaultNotificationEmail);
            }

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for financial info submitted email. Study ID: {StudyId}", study.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(email.TenantInfo)
                        .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoSubmitted.cshtml", email)
                );
                _logger.LogInformation("Financial info submitted email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send financial info submitted email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}
