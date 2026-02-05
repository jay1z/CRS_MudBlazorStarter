using Coravel.Mailer.Mail;
using CRS.Models.Emails;

namespace CRS.Models.Email;

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
        var from = _fromEmail ?? "no-reply@reservecloud.com";
        var subject = $"You're invited to join {_model.CustomerName} on {_model.TenantName}";

        this.To(_recipientEmail)
            .From(from)
            .Subject(subject)
            .View("~/Components/EmailTemplates/TeamInvitation.cshtml", _model);
    }
}
