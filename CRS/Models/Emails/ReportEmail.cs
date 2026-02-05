namespace CRS.Models.Emails;

/// <summary>
/// Email model for sending generated reports to clients.
/// </summary>
public class ReportEmail
{
    /// <summary>
    /// The generated report being sent.
    /// </summary>
    public required GeneratedReport Report { get; set; }
    
    /// <summary>
    /// The reserve study associated with the report.
    /// </summary>
    public ReserveStudy? ReserveStudy { get; set; }
    
    /// <summary>
    /// Optional personal message from the sender.
    /// </summary>
    public string? PersonalMessage { get; set; }
    
    /// <summary>
    /// The base URL of the application.
    /// </summary>
    public required string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Secure download URL for the report (time-limited SAS URL).
    /// </summary>
    public string? DownloadUrl { get; set; }
    
    /// <summary>
    /// Whether to include a download link in the email.
    /// </summary>
    public bool IncludeDownloadLink { get; set; } = true;
    
    /// <summary>
    /// Community/Association name for personalization.
    /// </summary>
    public string? CommunityName { get; set; }
    
    /// <summary>
    /// Display name for the report type (e.g., "Funding Plan", "Executive Summary").
    /// </summary>
    public string ReportTypeName { get; set; } = "Report";
    
    /// <summary>
    /// Report version for display.
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Tenant-specific branding and contact information for customizing emails.
    /// </summary>
    public TenantEmailInfo TenantInfo { get; set; } = new();
}
