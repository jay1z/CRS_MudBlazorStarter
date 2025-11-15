using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
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
            var subject = $"Financial Info Submitted for {study.Community?.Name}";
            var message = "Financial documents were submitted.";
            var recipients = new List<string> { "emailme@jasonzurowski.com" };
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);

            await _mailer.SendAsync(
            Mailable.AsInline<ReserveStudyEmail>()
            .To(recipients.ToArray())
            .Subject(subject)
            .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoSubmitted.cshtml", email)
            );
        }
    }
}
