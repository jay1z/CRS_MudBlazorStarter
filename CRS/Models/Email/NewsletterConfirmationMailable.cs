using Coravel.Mailer.Mail;

namespace CRS.Models.Email;

/// <summary>
/// Mailable for newsletter confirmation emails.
/// </summary>
public class NewsletterConfirmationMailable : Mailable<NewsletterConfirmationEmail>
{
    private readonly NewsletterConfirmationEmail _email;

    public NewsletterConfirmationMailable(NewsletterConfirmationEmail email)
    {
        _email = email;
    }

    public override void Build()
    {
        To(_email.Email)
            .From("newsletter@reservecloud.com")
            .Subject("🎉 Confirm your ALX Reserve Cloud newsletter subscription")
            .View("~/Components/EmailTemplates/Newsletter/ConfirmSubscription.cshtml", _email);
    }
}
