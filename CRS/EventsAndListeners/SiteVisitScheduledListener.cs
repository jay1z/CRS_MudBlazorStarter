using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Emails;
using Horizon.Services.Email;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Horizon.EventsAndListeners {
    public class SiteVisitScheduledListener : IListener<SiteVisitScheduledEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<SiteVisitScheduledListener> _logger;

        public SiteVisitScheduledListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<SiteVisitScheduledListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(SiteVisitScheduledEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            if (study is null) {
                _logger.LogWarning("SiteVisitScheduledEvent received with null ReserveStudy");
                return;
            }

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
                _logger.LogWarning("Could not find study {StudyId} for site visit scheduled email", broadcasted.ReserveStudy?.Id);
                return;
            }

            var siteVisitDate = broadcasted.SiteVisitDate;
            var message = $"Your site visit has been scheduled for {siteVisitDate:dddd, MMMM dd, yyyy} at {siteVisitDate:h:mm tt}. " +
                          $"Our specialist will visit {study.Community?.Name} to assess the property's common elements and building components.";

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, message);
            email.SiteVisitDate = siteVisitDate;

            // Build subject with tenant company name
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Site Visit Scheduled - {study.Community?.Name}";

            // Get recipient email - use contact email or HOA user email
            var recipients = new List<string>();
            if (!string.IsNullOrEmpty(study.Contact?.Email)) {
                recipients.Add(study.Contact.Email);
            }

            // Also notify study owner
            if (!string.IsNullOrEmpty(study.User?.Email) && 
                !recipients.Contains(study.User.Email, StringComparer.OrdinalIgnoreCase)) {
                recipients.Add(study.User.Email);
            }

            if (!recipients.Any()) {
                _logger.LogWarning("No email recipients found for SiteVisitScheduledEvent for study {StudyId}", study.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(email.TenantInfo)
                        .View("~/Components/EmailTemplates/SiteVisitScheduled.cshtml", email)
                );

                _logger.LogInformation("Site visit scheduled email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send site visit scheduled email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}
