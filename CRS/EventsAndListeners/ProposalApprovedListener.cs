using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
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
            var subject = $"Proposal Approved for {study.Community?.Name}";
            var message = "Your proposal has been approved.";
            var recipients = new List<string> { "emailme@jasonzurowski.com" };
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);

            await _mailer.SendAsync(
            Mailable.AsInline<ReserveStudyEmail>()
            .To(recipients.ToArray())
            .Subject(subject)
            .View("~/Components/EmailTemplates/ReserveStudyProposalApproved.cshtml", email)
            );
        }
    }
}
