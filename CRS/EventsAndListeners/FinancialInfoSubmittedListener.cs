using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class FinancialInfoSubmittedListener : IListener<FinancialInfoSubmittedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;

        public FinancialInfoSubmittedListener(IMailer mailer, IReserveStudyService reserveStudyService) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
        }

        public async Task HandleAsync(FinancialInfoSubmittedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            
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

            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(recipients.ToArray())
                    .Subject(subject)
                    .ReplyToTenant(email.TenantInfo)
                    .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoSubmitted.cshtml", email)
            );
        }
    }
}
