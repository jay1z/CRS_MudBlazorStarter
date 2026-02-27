using Coravel.Mailer.Mail;
using Horizon.Models.Emails;

namespace Horizon.Models.Email;

/// <summary>
/// Mailable for sending team invitation emails.
/// </summary>
public class TeamInvitationMailable : Mailable<TeamInvitationEmail>
{
    private readonly TeamInvitationEmail _model;
    private readonly string _recipientEmail;
    private readonly string? _fromEmail;

    public TeamInvitationMailable(TeamInvitationEmail model, string recipientEmail, string? fromEmail = null)
    {
        _model = model;
        _recipientEmail = recipientEmail;
        _fromEmail = fromEmail;
    }

    public override void Build()
    {
        // Use Azure Communication Services default domain if no custom sender provided
        var from = _fromEmail ?? "DoNotReply@4b9bbf9f-0f50-4984-9cf1-a70b8e8b1f32.azurecomm.net";
        var subject = $"You're invited to join {_model.CustomerName} on {_model.TenantName}";

        this.To(_recipientEmail)
            .From(from)
            .Subject(subject)
            .View("~/Components/EmailTemplates/TeamInvitation.cshtml", _model);
    }
}
