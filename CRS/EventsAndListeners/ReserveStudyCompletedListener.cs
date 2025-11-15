using System.Threading.Tasks;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
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
            var subject = $"Reserve Study Complete for {study.Community?.Name}";
            var message = "Your reserve study is complete.";
            var recipients = new List<string> { "emailme@jasonzurowski.com" };
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);

            await _mailer.SendAsync(
            Mailable.AsInline<ReserveStudyEmail>()
            .To(recipients.ToArray())
            .Subject(subject)
            .View("~/Components/EmailTemplates/ReserveStudyCompleted.cshtml", email)
            );
        }
    }
}
