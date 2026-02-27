using Coravel.Mailer.Mail;

using Horizon.Models.Emails;

namespace Horizon.Models.Email {
    public class ReserveStudyMailable : Mailable<ReserveStudyEmail> {
        public required ReserveStudyEmail ReserveStudyEmail { get; set; }

        public override void Build() {
            string recipient = "emailme@jasonzurowski.com";
            this.To(recipient)
                .From("no-reply@yourdomain.com")
                .Subject("New Reserve Study Created")
                .Html($"<html><body><h1>Welcome {recipient}</h1></body></html>")
                .Text($"Welcome {recipient}")
                .View("~/Components/EmailTemplates/ReserveStudyCreate.cshtml", ReserveStudyEmail);
        }
    }
}
