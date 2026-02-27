using System.Collections.Generic;
using Coravel.Mailer.Mail;

namespace Horizon.Services.Email
{
    // Simple generic mailable to send a subject + HTML/text body without a Razor view
    public class BasicMailable : Mailable<object>
    {
        public required string SubjectText { get; init; }
        public string? HtmlBody { get; init; }
        public string? TextBody { get; init; }
        public required IEnumerable<string> ToAddresses { get; init; }
        public string? FromAddress { get; init; }

        public override void Build()
        {
            foreach (var addr in ToAddresses)
            {
                this.To(addr);
            }

            if (!string.IsNullOrWhiteSpace(FromAddress))
            {
                this.From(FromAddress!);
            }

            this.Subject(SubjectText);

            if (!string.IsNullOrWhiteSpace(HtmlBody))
            {
                this.Html(HtmlBody!);
            }

            if (!string.IsNullOrWhiteSpace(TextBody))
            {
                this.Text(TextBody!);
            }
        }
    }
}
