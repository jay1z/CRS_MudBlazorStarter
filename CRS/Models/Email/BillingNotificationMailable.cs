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
            .From("DoNotReply@4b9bbf9f-0f50-4984-9cf1-a70b8e8b1f32.azurecomm.net")
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
        BillingNotificationType.TrialEnding => 
            $"⏰ Your Trial Ends in 3 Days - {_email.TenantName}",
        BillingNotificationType.UpcomingInvoice => 
            $"📋 Upcoming Invoice - {_email.TenantName}",
        BillingNotificationType.DisputeCreated => 
            $"🚨 URGENT: Chargeback Dispute Created - {_email.TenantName}",
        _ => $"Billing Notification - {_email.TenantName}"
    };

    private string GetTemplate() => _email.NotificationType switch
    {
        BillingNotificationType.PaymentFailed => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml",
        BillingNotificationType.GracePeriodStarted => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml",
        BillingNotificationType.AccountSuspended => "~/Components/EmailTemplates/Billing/AccountSuspended.cshtml",
        BillingNotificationType.AccountReactivated => "~/Components/EmailTemplates/Billing/AccountReactivated.cshtml",
        BillingNotificationType.TrialEnding => "~/Components/EmailTemplates/Billing/TrialEnding.cshtml",
        BillingNotificationType.UpcomingInvoice => "~/Components/EmailTemplates/Billing/TrialEnding.cshtml", // Reuse for now
        BillingNotificationType.DisputeCreated => "~/Components/EmailTemplates/Billing/AccountSuspended.cshtml", // Reuse for now
        _ => "~/Components/EmailTemplates/Billing/PaymentFailed.cshtml"
    };
}
