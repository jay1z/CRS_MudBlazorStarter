namespace Horizon.Models.Emails;

/// <summary>
/// Contains tenant-specific branding and contact information for emails.
/// Parsed from the tenant's BrandingJson configuration.
/// </summary>
public class TenantEmailInfo {
    /// <summary>
    /// The tenant's company/organization name
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// The email address emails should be sent from (Reply-To)
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Display name for the from address (e.g., "ABC Reserve Studies")
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Company phone number for contact info in emails
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Company website URL
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Company physical/mailing address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Company tagline or slogan
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// URL to the company logo for email headers
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex) for email styling
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex) for email styling
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// The tenant's subdomain for building URLs
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Default email address for system notifications when no specific recipient is available.
    /// Used as a fallback when a reserve study has no assigned specialist or contact.
    /// </summary>
    public string? DefaultNotificationEmail { get; set; }

    /// <summary>
    /// Whether this tenant has valid branding configured
    /// </summary>
    public bool HasBranding => !string.IsNullOrWhiteSpace(CompanyName) || !string.IsNullOrWhiteSpace(FromEmail);

    /// <summary>
    /// Gets a formatted "From" string for emails (e.g., "Company Name <email@domain.com>")
    /// </summary>
    public string GetFormattedFromAddress() {
        if (string.IsNullOrWhiteSpace(FromEmail)) {
            return string.Empty;
        }

        var displayName = FromName ?? CompanyName;
        if (string.IsNullOrWhiteSpace(displayName)) {
            return FromEmail;
        }

        return $"{displayName} <{FromEmail}>";
    }

    /// <summary>
    /// Creates a default TenantEmailInfo with platform branding
    /// </summary>
    public static TenantEmailInfo CreateDefault(string defaultCompanyName = "ALX Reserve Cloud") {
        return new TenantEmailInfo {
            CompanyName = defaultCompanyName,
            FromEmail = null, // Use system default
            PrimaryColor = "#667eea",
            SecondaryColor = "#764ba2"
        };
    }
}
