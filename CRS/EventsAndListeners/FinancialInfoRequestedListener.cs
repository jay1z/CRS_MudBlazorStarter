using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class FinancialInfoRequestedListener : IListener<FinancialInfoRequestedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;

        public FinancialInfoRequestedListener(IMailer mailer, IReserveStudyService reserveStudyService) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
        }

        public async Task HandleAsync(FinancialInfoRequestedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            
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

            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(recipients.ToArray())
                    .Subject(subject)
                    .ReplyToTenant(email.TenantInfo)
                    .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoRequested.cshtml", email)
            );
        }
    }
}
