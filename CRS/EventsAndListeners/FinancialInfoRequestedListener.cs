using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
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
            var subject = $"Financial Info Requested for {study.Community?.Name}";
            var message = "We need financial documents to proceed.";
            var recipients = new List<string> { "emailme@jasonzurowski.com" };
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);

            await _mailer.SendAsync(
            Mailable.AsInline<ReserveStudyEmail>()
            .To(recipients.ToArray())
            .Subject(subject)
            .View("~/Components/EmailTemplates/ReserveStudyFinancialInfoRequested.cshtml", email)
            );
        }
    }
}
