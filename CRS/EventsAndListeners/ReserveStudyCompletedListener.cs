using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class ReserveStudyCompletedListener : IListener<ReserveStudyCompletedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;

        public ReserveStudyCompletedListener(IMailer mailer, IReserveStudyService reserveStudyService) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
        }

        public async Task HandleAsync(ReserveStudyCompletedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            
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

            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(recipients.ToArray())
                    .Subject(subject)
                    .ReplyToTenant(email.TenantInfo)
                    .View("~/Components/EmailTemplates/ReserveStudyCompleted.cshtml", email)
            );
        }
    }
}
