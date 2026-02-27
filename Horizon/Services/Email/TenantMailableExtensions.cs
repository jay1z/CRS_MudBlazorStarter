using Coravel.Mailer.Mail;
using Horizon.Models.Emails;

namespace Horizon.Services.Email;

/// <summary>
/// Extension methods for Coravel Mailable to support tenant-specific sender configuration
/// </summary>
public static class TenantMailableExtensions {
    /// <summary>
    /// Configures the email with tenant-specific From address if available.
    /// Falls back to system default if tenant email is not configured.
    /// </summary>
    /// <typeparam name="T">The type of the Mailable model</typeparam>
    /// <param name="mailable">The Mailable instance</param>
    /// <param name="tenantInfo">Tenant email information</param>
    /// <returns>The configured Mailable</returns>
    public static Mailable<T> FromTenant<T>(this Mailable<T> mailable, TenantEmailInfo? tenantInfo) {
        if (tenantInfo == null || string.IsNullOrWhiteSpace(tenantInfo.FromEmail)) {
            // No tenant email configured, use system default (configured in appsettings)
            return mailable;
        }

        // Use the tenant's configured From address
        return mailable.From(new Coravel.Mailer.Mail.MailRecipient(tenantInfo.FromEmail, tenantInfo.FromName ?? tenantInfo.CompanyName));
    }

    /// <summary>
    /// Configures the email with Reply-To address set to the tenant's email.
    /// This is useful when the From address must be a verified sender but you want
    /// replies to go to the tenant.
    /// </summary>
    /// <typeparam name="T">The type of the Mailable model</typeparam>
    /// <param name="mailable">The Mailable instance</param>
    /// <param name="tenantInfo">Tenant email information</param>
    /// <returns>The configured Mailable</returns>
    public static Mailable<T> ReplyToTenant<T>(this Mailable<T> mailable, TenantEmailInfo? tenantInfo) {
        if (tenantInfo == null || string.IsNullOrWhiteSpace(tenantInfo.FromEmail)) {
            return mailable;
        }

        return mailable.ReplyTo(tenantInfo.FromEmail);
    }
}
