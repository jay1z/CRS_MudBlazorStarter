using Coravel.Mailer.Mail;
using Horizon.Models.Emails;

namespace Horizon.Services.Email.Mailables;

/// <summary>
/// Mailable for trial ending warning emails.
/// Sent at 7, 3, and 1 day(s) before trial expiration.
/// </summary>
public class TrialEndingMailable : Mailable<BillingNotificationEmail>
{
    private readonly BillingNotificationEmail _email;
    private readonly int _daysRemaining;
    private readonly bool _isUrgent;

    public TrialEndingMailable(BillingNotificationEmail email, int daysRemaining, bool isUrgent)
    {
        _email = email;
        _daysRemaining = daysRemaining;
        _isUrgent = isUrgent;
    }

    public override void Build()
    {
        var subject = _isUrgent
            ? $"⚠️ Your Trial Ends Tomorrow - {_email.TenantName}"
            : $"Your Trial Ends in {_daysRemaining} Days - {_email.TenantName}";

        To(_email.OwnerEmail)
            .From("DoNotReply@4b9bbf9f-0f50-4984-9cf1-a70b8e8b1f32.azurecomm.net")
            .Subject(subject)
            .View("~/Components/EmailTemplates/Billing/TrialEnding.cshtml", _email);
    }
}

/// <summary>
/// Mailable for trial expired notification.
/// Sent when the trial period has ended.
/// </summary>
public class TrialExpiredMailable : Mailable<BillingNotificationEmail>
{
    private readonly BillingNotificationEmail _email;

    public TrialExpiredMailable(BillingNotificationEmail email)
    {
        _email = email;
    }

    public override void Build()
    {
        To(_email.OwnerEmail)
            .From("DoNotReply@4b9bbf9f-0f50-4984-9cf1-a70b8e8b1f32.azurecomm.net")
            .Subject($"Your Trial Has Expired - {_email.TenantName}")
            .View("~/Components/EmailTemplates/Billing/TrialExpired.cshtml", _email);
    }
}
