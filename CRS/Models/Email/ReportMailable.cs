using Coravel.Mailer.Mail;

using Horizon.Models.Emails;

namespace Horizon.Models.Email;

/// <summary>
/// Mailable for sending generated reports to clients.
/// </summary>
public class ReportMailable : Mailable<ReportEmail>
{
    private readonly ReportEmail _reportEmail;
    private readonly string _recipientEmail;
    private readonly string? _ccEmail;
    private readonly string _subject;
    private readonly byte[]? _attachmentData;
    private readonly string? _attachmentFilename;

    public ReportMailable(
        ReportEmail reportEmail, 
        string recipientEmail,
        string subject,
        string? ccEmail = null,
        byte[]? attachmentData = null,
        string? attachmentFilename = null)
    {
        _reportEmail = reportEmail;
        _recipientEmail = recipientEmail;
        _ccEmail = ccEmail;
        _subject = subject;
        _attachmentData = attachmentData;
        _attachmentFilename = attachmentFilename;
    }

    public override void Build()
    {
        var fromEmail = _reportEmail.TenantInfo?.FromEmail ?? "no-reply@reservecloud.com";

        var mailable = this
            .To(_recipientEmail)
            .From(fromEmail)
            .Subject(_subject)
            .View("~/Components/EmailTemplates/ReportSent.cshtml", _reportEmail);

        // Add CC if provided
        if (!string.IsNullOrWhiteSpace(_ccEmail))
        {
            mailable.Cc(new[] { new MailRecipient(_ccEmail) });
        }

        // Add attachment if provided
        if (_attachmentData != null && !string.IsNullOrWhiteSpace(_attachmentFilename))
        {
            mailable.Attach(new Attachment
            {
                Name = _attachmentFilename,
                Bytes = _attachmentData
            });
        }
    }
}
