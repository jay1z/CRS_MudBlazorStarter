using Coravel.Mailer.Mail;
using CRS.Models.Emails;

namespace CRS.Models.Email;

/// <summary>
/// Mailable for billing-related notifications (payment failed, suspension, reactivation).
/// </summary>
public class BillingNotificationMailable : Mailable<BillingNotificationEmail>
{
    private readonly BillingNotificationEmail _email;

    public BillingNotificationMailable(BillingNotificationEmail email)
    {
        _email = email;
    }

    public override void Build()
    {
        var subject = GetSubject();
        var template = GetTemplate();

        To(_email.OwnerEmail)
            .From("billing@reservecloud.com")
            .Subject(subject)
            .View(template, _email);
    }

    private string GetSubject() => _email.NotificationType switch
    {
        BillingNotificationType.PaymentFailed => 
            $"⚠️ Action Required: Payment Failed for {_email.TenantName}",
        BillingNotificationType.GracePeriodStarted => 
            $"⚠️ Payment Issue: {_email.TenantName} Account Notice",
        BillingNotificationType.AccountSuspended => 
            $"🔒 Account Suspended: {_email.TenantName}",
        BillingNotificationType.AccountReactivated => 
            $"✅ Welcome Back! {_email.TenantName} Account Reactivated",
        _ => $"Billing Notification - {_email.TenantName}"
    };

    private string GetTemplate() => _email.NotificationType switch
    {
        BillingNotificationType.PaymentFailed => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml",
        BillingNotificationType.GracePeriodStarted => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml",
        BillingNotificationType.AccountSuspended => "~/Components/EmailTemplates/Billing/AccountSuspended.cshtml",
        BillingNotificationType.AccountReactivated => "~/Components/EmailTemplates/Billing/AccountReactivated.cshtml",
        _ => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml"
    };
}
