using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class SiteVisitScheduledListener : IListener<SiteVisitScheduledEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ILogger<SiteVisitScheduledListener> _logger;

        public SiteVisitScheduledListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            ILogger<SiteVisitScheduledListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _logger = logger;
        }

        public async Task HandleAsync(SiteVisitScheduledEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            if (study is null) {
                _logger.LogWarning("SiteVisitScheduledEvent received with null ReserveStudy");
                return;
            }

            var siteVisitDate = broadcasted.SiteVisitDate;
            var subject = $"Site Visit Scheduled - {study.Community?.Name}";
            var message = $"Your site visit has been scheduled for {siteVisitDate:dddd, MMMM dd, yyyy} at {siteVisitDate:h:mm tt}. " +
                          $"Our specialist will visit {study.Community?.Name} to assess the property's common elements and building components.";

            // Get recipient email - use contact email or HOA user email
            var recipients = new List<string>();
            if (!string.IsNullOrEmpty(study.Contact?.Email)) {
                recipients.Add(study.Contact.Email);
            }

            if (!recipients.Any()) {
                _logger.LogWarning("No email recipients found for SiteVisitScheduledEvent for study {StudyId}", study.Id);
                return;
            }

            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);
            email.SiteVisitDate = siteVisitDate;

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .View("~/Components/EmailTemplates/SiteVisitScheduled.cshtml", email)
                );

                _logger.LogInformation("Site visit scheduled email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", recipients), study.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send site visit scheduled email for study {StudyId}", study.Id);
            }
        }
    }
}
