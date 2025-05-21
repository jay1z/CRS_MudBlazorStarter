using System.Threading.Tasks;
using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners
{
    public class ProposalSentListener : IListener<ProposalSentEvent>
    {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;

        public ProposalSentListener(IMailer mailer, IReserveStudyService reserveStudyService)
        {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
        }

        public async Task HandleAsync(ProposalSentEvent broadcasted)
        {
            var study = broadcasted.ReserveStudy;
            var proposal = broadcasted.Proposal;
            
            var subject = $"Reserve Study Proposal for {study.Community?.Name}";
            var message = $"A proposal has been sent for your review. Please review the proposal details and respond accordingly.";
            
            var recipients = new List<string>() { "emailme@jasonzurowski.com" };
            
            // Add point of contact
            if (study.PointOfContact?.Email != null)
            {
                //recipients.Add(study.PointOfContact.Email);
            }
            
            // Add specialist
            if (study.Specialist?.Email != null)
            {
                //recipients.Add(study.Specialist.Email);
            }
            
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);
            
            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(recipients.ToArray())
                    .Subject(subject)
                    .View("~/Components/EmailTemplates/ReserveStudyProposalSent.cshtml", email)
            );
        }
    }
}