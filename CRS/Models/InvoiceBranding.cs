using System.Text.Json;

namespace Horizon.Models;

/// <summary>
/// Represents the branding configuration for invoice PDF generation.
/// </summary>
public class InvoiceBranding
{
    /// <summary>
    /// Company/organization name displayed on invoices.
    /// </summary>
    public string CompanyName { get; set; } = "Reserve Study Company";

    /// <summary>
    /// Tagline/slogan shown under company name.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// Company address for invoice header.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Company phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Company email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Company website URL.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// URL to the company logo.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex format).
    /// </summary>
    public string PrimaryColor { get; set; } = "#667eea";

    /// <summary>
    /// Secondary brand color (hex format).
    /// </summary>
    public string SecondaryColor { get; set; } = "#764ba2";

    /// <summary>
    /// Footer text for invoices.
    /// </summary>
    public string FooterText { get; set; } = "Thank you for your business!";

    /// <summary>
    /// Default payment terms.
    /// </summary>
    public string? DefaultTerms { get; set; }

    /// <summary>
    /// Payment instructions to display on invoice.
    /// </summary>
    public string? PaymentInstructions { get; set; }

    /// <summary>
    /// Whether to show a PAID watermark on paid invoices.
    /// </summary>
    public bool ShowPaidWatermark { get; set; } = true;

    /// <summary>
    /// Whether branding is configured (vs using defaults).
    /// </summary>
    public bool HasBranding { get; set; }

    /// <summary>
    /// Creates an InvoiceBranding from TenantInvoiceSettings and optionally Tenant.
    /// </summary>
    public static InvoiceBranding FromSettings(TenantInvoiceSettings? settings, Tenant? tenant)
    {
        var branding = new InvoiceBranding();

        // First, try to use tenant's BrandingJson if available
        if (tenant != null && !string.IsNullOrWhiteSpace(tenant.BrandingJson))
        {
            try
            {
                var brandingData = JsonSerializer.Deserialize<TenantBrandingData>(tenant.BrandingJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (brandingData != null)
                {
                    branding.LogoUrl = brandingData.LogoUrl;
                    branding.Address = brandingData.Address;
                    branding.Phone = brandingData.Phone;
                    branding.Email = brandingData.Email;
                    branding.Website = brandingData.Website;
                    branding.Tagline = brandingData.Tagline;
                    
                    if (!string.IsNullOrWhiteSpace(brandingData.Primary))
                        branding.PrimaryColor = brandingData.Primary;
                    if (!string.IsNullOrWhiteSpace(brandingData.Secondary))
                        branding.SecondaryColor = brandingData.Secondary;
                }
            }
            catch (JsonException)
            {
                // Ignore parse errors, use defaults
            }
        }

        // Set company name from tenant
        if (tenant != null)
        {
            branding.CompanyName = tenant.Name;
        }

        // Override with invoice-specific settings if available
        if (settings != null && settings.UseTenantBranding)
        {
            branding.HasBranding = true;

            if (!string.IsNullOrWhiteSpace(settings.CompanyName))
                branding.CompanyName = settings.CompanyName;
            if (!string.IsNullOrWhiteSpace(settings.CompanyAddress))
                branding.Address = settings.CompanyAddress;
            if (!string.IsNullOrWhiteSpace(settings.CompanyPhone))
                branding.Phone = settings.CompanyPhone;
            if (!string.IsNullOrWhiteSpace(settings.CompanyEmail))
                branding.Email = settings.CompanyEmail;
            if (!string.IsNullOrWhiteSpace(settings.CompanyWebsite))
                branding.Website = settings.CompanyWebsite;
            if (!string.IsNullOrWhiteSpace(settings.LogoUrl))
                branding.LogoUrl = settings.LogoUrl;
            if (!string.IsNullOrWhiteSpace(settings.Tagline))
                branding.Tagline = settings.Tagline;
            if (!string.IsNullOrWhiteSpace(settings.PrimaryColor))
                branding.PrimaryColor = settings.PrimaryColor;
            if (!string.IsNullOrWhiteSpace(settings.SecondaryColor))
                branding.SecondaryColor = settings.SecondaryColor;
            if (!string.IsNullOrWhiteSpace(settings.FooterText))
                branding.FooterText = settings.FooterText;
            if (!string.IsNullOrWhiteSpace(settings.DefaultTerms))
                branding.DefaultTerms = settings.DefaultTerms;
            if (!string.IsNullOrWhiteSpace(settings.PaymentInstructions))
                branding.PaymentInstructions = settings.PaymentInstructions;
            
            branding.ShowPaidWatermark = settings.ShowPaidWatermark;
        }

        return branding;
    }

    /// <summary>
    /// Creates a default InvoiceBranding with minimal configuration.
    /// </summary>
    public static InvoiceBranding CreateDefault(string companyName = "Reserve Study Company")
    {
        return new InvoiceBranding
        {
            CompanyName = companyName,
            HasBranding = false
        };
    }

    /// <summary>
    /// Helper class for parsing tenant branding JSON.
    /// </summary>
    private sealed class TenantBrandingData
    {
        public string? Primary { get; set; }
        public string? Secondary { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Tagline { get; set; }
        public string? FooterText { get; set; }
    }
}
