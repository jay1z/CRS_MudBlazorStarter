namespace Horizon.Models.Emails;

/// <summary>
/// Email model for sending invoice notifications and reminders.
/// </summary>
public class InvoiceEmail
{
    public required Invoice Invoice { get; set; }
    
    public ReserveStudy? ReserveStudy { get; set; }
    
    public string? AdditionalMessage { get; set; }
    
    public required string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL where the client can view/pay the invoice online.
    /// </summary>
    public string? InvoiceViewUrl { get; set; }
    
    /// <summary>
    /// Indicates if this is a reminder email for an overdue invoice.
    /// </summary>
    public bool IsReminder { get; set; }
    
    /// <summary>
    /// Number of days past due (for overdue reminders).
    /// </summary>
    public int DaysPastDue { get; set; }

    /// <summary>
    /// Tenant-specific branding and contact information for customizing emails.
    /// </summary>
    public TenantEmailInfo TenantInfo { get; set; } = new();
}
