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
    public class ProposalApprovedListener : IListener<ProposalApprovedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<ProposalApprovedListener> _logger;

        public ProposalApprovedListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<ProposalApprovedListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(ProposalApprovedEvent broadcasted) {
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
                _logger.LogWarning("Could not find study {StudyId} for proposal approved email", broadcasted.ReserveStudy.Id);
                return;
            }

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, 
                "Your proposal has been approved. We will begin processing your reserve study shortly.");

            // Build subject with tenant company name
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Proposal Approved for {study.Community?.Name}";

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

            // Fallback to tenant's default notification email
            if (recipients.Count == 0 && !string.IsNullOrEmpty(email.TenantInfo?.DefaultNotificationEmail)) {
                recipients.Add(email.TenantInfo.DefaultNotificationEmail);
            }

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for proposal approved email. Study ID: {StudyId}", study.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(email.TenantInfo)
                        .View("~/Components/EmailTemplates/ReserveStudyProposalApproved.cshtml", email)
                );
                _logger.LogInformation("Proposal approved email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send proposal approved email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}
