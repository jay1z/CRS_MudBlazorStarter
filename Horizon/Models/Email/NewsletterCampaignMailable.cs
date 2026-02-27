using Coravel.Mailer.Mail;

namespace Horizon.Models.Email;

/// <summary>
/// Mailable for newsletter campaign emails.
/// </summary>
public class NewsletterCampaignMailable : Mailable<NewsletterCampaignEmail>
{
    private readonly NewsletterCampaignEmail _email;

    public NewsletterCampaignMailable(NewsletterCampaignEmail email)
    {
        _email = email;
    }

    public override void Build()
    {
        To(_email.SubscriberEmail)
            .From("newsletter@reservecloud.com")
            .Subject(_email.Subject)
            .View("~/Components/EmailTemplates/Newsletter/CampaignEmail.cshtml", _email);
    }
}
