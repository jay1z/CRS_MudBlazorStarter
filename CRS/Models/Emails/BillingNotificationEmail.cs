namespace CRS.Models.Emails;

/// <summary>
/// Model for billing-related notification emails (payment failed, suspension, reactivation).
/// </summary>
public class BillingNotificationEmail
{
    /// <summary>
    /// Type of billing notification.
    /// </summary>
    public required BillingNotificationType NotificationType { get; set; }

    /// <summary>
    /// Tenant/Company name.
    /// </summary>
    public required string TenantName { get; set; }

    /// <summary>
    /// Owner's email address.
    /// </summary>
    public required string OwnerEmail { get; set; }

    /// <summary>
    /// Owner's name (if available).
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Subscription plan name.
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// Amount due (for payment failed notifications).
    /// </summary>
    public decimal? AmountDue { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Date when the grace period ends (for suspension warnings).
    /// </summary>
    public DateTime? GracePeriodEndsAt { get; set; }

    /// <summary>
    /// Date when the account was suspended.
    /// </summary>
    public DateTime? SuspendedAt { get; set; }

    /// <summary>
    /// Date when the account was reactivated.
    /// </summary>
    public DateTime? ReactivatedAt { get; set; }

    /// <summary>
    /// Number of times the account has been reactivated.
    /// </summary>
    public int ReactivationCount { get; set; }

    /// <summary>
    /// URL to update payment method.
    /// </summary>
    public string? UpdatePaymentUrl { get; set; }

    /// <summary>
    /// URL to the billing portal.
    /// </summary>
    public string? BillingPortalUrl { get; set; }

    /// <summary>
    /// URL to the dashboard/login page.
    /// </summary>
    public string? DashboardUrl { get; set; }

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "support@reservecloud.com";

    /// <summary>
    /// Number of days remaining in grace period.
    /// </summary>
    public int? DaysRemaining => GracePeriodEndsAt.HasValue 
        ? Math.Max(0, (int)(GracePeriodEndsAt.Value - DateTime.UtcNow).TotalDays) 
        : null;
}

/// <summary>
/// Types of billing notifications.
/// </summary>
public enum BillingNotificationType
{
    /// <summary>
    /// Payment attempt failed.
    /// </summary>
    PaymentFailed,

    /// <summary>
    /// Account entering grace period (past due).
    /// </summary>
    GracePeriodStarted,

    /// <summary>
    /// Account has been suspended due to non-payment.
    /// </summary>
    AccountSuspended,

    /// <summary>
    /// Account has been reactivated after payment.
    /// </summary>
    AccountReactivated
}
