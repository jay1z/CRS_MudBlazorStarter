namespace CRS.Models;

/// <summary>
/// Settings for customizing PDF document generation (proposals, reports, etc.)
/// These settings are stored as part of the tenant's branding configuration.
/// 
/// NOTE: Company contact info (name, phone, email, website, address, logo) is stored
/// in the parent BrandingPayload to avoid duplication. Use GetEffectiveSettings() 
/// in ProposalPdfService to merge these values.
/// </summary>
public class PdfSettings
{
    // ═══════════════════════════════════════════════════════════════
    // PDF-SPECIFIC OVERRIDES (Optional - falls back to BrandingPayload)
    // Set these only if you want PDFs to show different info than general settings
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Override company name for PDFs. If null, uses Tenant.Name.
    /// </summary>
    public string? CompanyNameOverride { get; set; }
    
    /// <summary>
    /// Override company tagline for PDFs. If null, uses BrandingPayload.CompanyTagline.
    /// </summary>
    public string? CompanyTaglineOverride { get; set; }
    
    /// <summary>
    /// Override logo URL for PDFs. If null, uses tenant logo or BrandingPayload.CompanyLogoUrl.
    /// </summary>
    public string? CompanyLogoUrlOverride { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // COLOR SCHEME (PDF-specific - may differ from web theme)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// If true, PDF colors will use the web theme colors from BrandingPayload.
    /// If false, uses the colors defined below.
    /// </summary>
    public bool UseThemeColors { get; set; } = true;
    
    // These are only used if UseThemeColors = false
    public string PrimaryColor { get; set; } = "#667eea";
    public string SecondaryColor { get; set; } = "#764ba2";
    public string AccentColor { get; set; } = "#10b981";
    public string TextColor { get; set; } = "#333333";
    public string MutedTextColor { get; set; } = "#666666";

    // ═══════════════════════════════════════════════════════════════
    // TYPOGRAPHY
    // ═══════════════════════════════════════════════════════════════

    public string FontFamily { get; set; } = "Arial";
    public int BaseFontSize { get; set; } = 11;
    public int HeaderFontSize { get; set; } = 24;
    public int SectionHeaderFontSize { get; set; } = 14;

    // ═══════════════════════════════════════════════════════════════
    // HEADER & FOOTER (Shared defaults)
    // ═══════════════════════════════════════════════════════════════

    public bool ShowLogoInHeader { get; set; } = true;
    public bool ShowCompanyNameInHeader { get; set; } = true;
    public bool ShowFooter { get; set; } = true;
    public bool ShowPageNumbers { get; set; } = true;
    public bool ShowGeneratedDate { get; set; } = true;
    public string? FooterText { get; set; }
    public bool ShowContactInfoInFooter { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // MARGINS (in points, 72 points = 1 inch)
    // ═══════════════════════════════════════════════════════════════

    public int MarginTop { get; set; } = 50;
    public int MarginBottom { get; set; } = 50;
    public int MarginLeft { get; set; } = 50;
    public int MarginRight { get; set; } = 50;

    // ═══════════════════════════════════════════════════════════════
    // PROPOSAL-SPECIFIC SETTINGS
    // ═══════════════════════════════════════════════════════════════

    public ProposalPdfSettings Proposal { get; set; } = new();

    // ═══════════════════════════════════════════════════════════════
    // FINAL REPORT-SPECIFIC SETTINGS
    // ═══════════════════════════════════════════════════════════════

    public ReportPdfSettings Report { get; set; } = new();

    /// <summary>
    /// Creates default PDF settings
    /// </summary>
    public static PdfSettings Default => new();
}

/// <summary>
/// Settings specific to proposal PDF generation
/// </summary>
public class ProposalPdfSettings
{
    // Title & Branding
    public string Title { get; set; } = "RESERVE STUDY PROPOSAL";
    public string? Subtitle { get; set; }

    // Electronic Acceptance
    public bool ShowElectronicAcceptanceNotice { get; set; } = true;
    public string? ElectronicAcceptanceText { get; set; }

    // Terms and Conditions
    public bool ShowTermsAndConditions { get; set; } = true;
    public List<string> TermsAndConditions { get; set; } = new()
    {
        "This proposal is valid for 30 days from the proposal date.",
        "Payment terms: 50% upon acceptance, 50% upon completion.",
        "The reserve study will be completed within 60-90 days of acceptance.",
        "Site visit coordination will be arranged after proposal acceptance.",
        "Final deliverables include a comprehensive reserve study report and funding plan."
    };

    // Section Visibility
    public bool ShowCommunityInfo { get; set; } = true;
    public bool ShowContactInfo { get; set; } = true;
    public bool ShowSpecialistInfo { get; set; } = true;
    public bool ShowServiceLevel { get; set; } = true;
    public bool ShowDeliveryTimeframe { get; set; } = true;
    public bool ShowAdditionalServices { get; set; } = true;
    public bool ShowPaymentTerms { get; set; } = true;
    public bool ShowScopeOfWork { get; set; } = true;
    public bool ShowEstimatedCost { get; set; } = true;
}

/// <summary>
/// Settings specific to final reserve study report PDF generation
/// </summary>
public class ReportPdfSettings
{
    // Title & Branding
    public string Title { get; set; } = "RESERVE STUDY REPORT";
    public string? Subtitle { get; set; }
    public bool ShowCoverPage { get; set; } = true;
    public string? CoverPageImageUrl { get; set; }

    // Table of Contents
    public bool ShowTableOfContents { get; set; } = true;
    public bool ShowPageNumbersInToc { get; set; } = true;

    // Executive Summary
    public bool ShowExecutiveSummary { get; set; } = true;
    public string ExecutiveSummaryTitle { get; set; } = "Executive Summary";

    // Section Visibility
    public bool ShowCommunityInfo { get; set; } = true;
    public bool ShowPropertyDescription { get; set; } = true;
    public bool ShowComponentInventory { get; set; } = true;
    public bool ShowConditionAssessments { get; set; } = true;
    public bool ShowFundingAnalysis { get; set; } = true;
    public bool ShowFundingPlan { get; set; } = true;
    public bool ShowReserveSchedule { get; set; } = true;
    public bool ShowExpenditureHistory { get; set; } = true;
    public bool ShowPhotos { get; set; } = true;
    public bool ShowAppendices { get; set; } = true;

    // Funding Plan Options
    public bool ShowMultipleFundingScenarios { get; set; } = true;
    public int FundingProjectionYears { get; set; } = 30;
    public bool ShowGraphsAndCharts { get; set; } = true;

    // Component Details
    public bool ShowComponentPhotos { get; set; } = true;
    public bool ShowComponentConditionRatings { get; set; } = true;
    public bool ShowUsefulLifeData { get; set; } = true;
    public bool ShowReplacementCosts { get; set; } = true;

    // Appendices
    public bool ShowMethodologyAppendix { get; set; } = true;
    public bool ShowGlossaryAppendix { get; set; } = true;
    public bool ShowAssumptionsAppendix { get; set; } = true;

    // Disclaimer & Legal
    public bool ShowDisclaimer { get; set; } = true;
    public string? DisclaimerText { get; set; }
    public bool ShowCertification { get; set; } = true;
    public string? CertificationText { get; set; }

    // Watermark
    public bool ShowDraftWatermark { get; set; } = false;
    public string DraftWatermarkText { get; set; } = "DRAFT";
}

/// <summary>
/// Effective PDF settings with all values resolved (merges PdfSettings with BrandingPayload)
/// </summary>
public class EffectivePdfSettings
{
    // Resolved company info
    public string? CompanyName { get; set; }
    public string? CompanyTagline { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? CompanyAddress { get; set; }

    // Resolved colors
    public string PrimaryColor { get; set; } = "#667eea";
    public string SecondaryColor { get; set; } = "#764ba2";
    public string AccentColor { get; set; } = "#10b981";
    public string TextColor { get; set; } = "#333333";
    public string MutedTextColor { get; set; } = "#666666";

    // Typography
    public string FontFamily { get; set; } = "Arial";
    public int BaseFontSize { get; set; } = 11;
    public int HeaderFontSize { get; set; } = 24;
    public int SectionHeaderFontSize { get; set; } = 14;

    // Header & Footer
    public bool ShowLogoInHeader { get; set; } = true;
    public bool ShowCompanyNameInHeader { get; set; } = true;
    public bool ShowFooter { get; set; } = true;
    public bool ShowPageNumbers { get; set; } = true;
    public bool ShowGeneratedDate { get; set; } = true;
    public string? FooterText { get; set; }
    public bool ShowContactInfoInFooter { get; set; } = true;

    // Margins
    public int MarginTop { get; set; } = 50;
    public int MarginBottom { get; set; } = 50;
    public int MarginLeft { get; set; } = 50;
    public int MarginRight { get; set; } = 50;

    // Document-specific settings
    public ProposalPdfSettings Proposal { get; set; } = new();
    public ReportPdfSettings Report { get; set; } = new();
}
