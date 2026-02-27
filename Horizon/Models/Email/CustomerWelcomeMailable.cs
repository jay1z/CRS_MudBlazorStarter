using Coravel.Mailer.Mail;
using Horizon.Models.Emails;

namespace Horizon.Models.Email;

/// <summary>
/// Mailable for sending welcome email to new customer account registrations.
/// </summary>
public class CustomerWelcomeMailable : Mailable<CustomerWelcomeEmail>
{
    private readonly CustomerWelcomeEmail _model;
    private readonly string _recipientEmail;
    private readonly string? _fromEmail;

    public CustomerWelcomeMailable(CustomerWelcomeEmail model, string recipientEmail, string? fromEmail = null)
    {
        _model = model;
        _recipientEmail = recipientEmail;
        _fromEmail = fromEmail;
    }

    public override void Build()
    {
        var from = _fromEmail ?? "no-reply@reservecloud.com";
        var subject = $"Welcome to {_model.TenantName}!";

        this.To(_recipientEmail)
            .From(from)
            .Subject(subject)
            .View("~/Components/EmailTemplates/CustomerWelcome.cshtml", _model);
    }
}
