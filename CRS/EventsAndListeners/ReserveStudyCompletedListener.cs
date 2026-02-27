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
    public class ReserveStudyCompletedListener : IListener<ReserveStudyCompletedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<ReserveStudyCompletedListener> _logger;

        public ReserveStudyCompletedListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<ReserveStudyCompletedListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(ReserveStudyCompletedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;

            // Reload study with all needed navigation properties
            await using var db = await _dbFactory.CreateDbContextAsync();
            study = await db.ReserveStudies
                .Include(s => s.User)
                .Include(s => s.Community)
                .Include(s => s.Contact)
                .Include(s => s.PropertyManager)
                .FirstOrDefaultAsync(s => s.Id == study.Id);

            if (study == null)
            {
                _logger.LogWarning("Could not find study {StudyId} for reserve study completed email", broadcasted.ReserveStudy.Id);
                return;
            }

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, 
                "Your reserve study has been completed! You can now access your full reserve study report online.");

            // Build subject with tenant company name
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Reserve Study Complete - {study.Community?.Name}";

            var recipients = new List<string>();

            // Add study owner
            if (!string.IsNullOrEmpty(study.User?.Email)) {
                recipients.Add(study.User.Email);
            }

            // Add point of contact
            if (!string.IsNullOrEmpty(study.PointOfContact?.Email) && 
                !recipients.Contains(study.PointOfContact.Email, StringComparer.OrdinalIgnoreCase)) {
                recipients.Add(study.PointOfContact.Email);
            }

            // Add contact
            if (!string.IsNullOrEmpty(study.Contact?.Email) && 
                !recipients.Contains(study.Contact.Email, StringComparer.OrdinalIgnoreCase)) {
                recipients.Add(study.Contact.Email);
            }

            // Fallback to tenant's default notification email
            if (recipients.Count == 0 && !string.IsNullOrEmpty(email.TenantInfo?.DefaultNotificationEmail)) {
                recipients.Add(email.TenantInfo.DefaultNotificationEmail);
            }

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for reserve study completed email. Study ID: {StudyId}", study.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(email.TenantInfo)
                        .View("~/Components/EmailTemplates/ReserveStudyCompleted.cshtml", email)
                );
                _logger.LogInformation("Reserve study completed email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send reserve study completed email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}
