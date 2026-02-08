using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Data;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.EventsAndListeners {
    public class FinancialInfoRequestedListener : IListener<FinancialInfoRequestedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<FinancialInfoRequestedListener> _logger;

        public FinancialInfoRequestedListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<FinancialInfoRequestedListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(FinancialInfoRequestedEvent broadcasted) {
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
                _logger.LogWarning("Could not find study {StudyId} for financial info requested email", broadcasted.ReserveStudy.Id);
                return;
            }

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, 
                "We need your community's financial documents to proceed with your reserve study. Please log in to submit the required information.");

            // Build subject with tenant company name
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Financial Information Requested - {study.Community?.Name}";

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
                _logger.LogWarning("No recipients found for financial info requested email. Study ID: {StudyId}", study.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(email.TenantInfo)
                        .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoRequested.cshtml", email)
                );
                _logger.LogInformation("Financial info requested email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send financial info requested email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}
