using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class ProposalApprovedListener : IListener<ProposalApprovedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;

        public ProposalApprovedListener(IMailer mailer, IReserveStudyService reserveStudyService) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
        }

        public async Task HandleAsync(ProposalApprovedEvent broadcasted) {
            var study = broadcasted.ReserveStudy;
            
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
            
            // Fallback
            if (recipients.Count == 0) {
                recipients.Add("emailme@jasonzurowski.com"); // TODO: Configure default
            }

            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(recipients.ToArray())
                    .Subject(subject)
                    .ReplyToTenant(email.TenantInfo)
                    .View("~/Components/EmailTemplates/ReserveStudyProposalApproved.cshtml", email)
            );
        }
    }
}
