namespace CRS.Models.NarrativeTemplates;

/// <summary>
/// Complete context for rendering a reserve study narrative report.
/// This DTO aggregates all data needed for placeholder replacement and section rendering.
/// </summary>
public class ReserveStudyReportContext
{
    /// <summary>Association/community information.</summary>
    public AssociationInfo Association { get; set; } = new();

    /// <summary>Study metadata and dates.</summary>
    public StudyInfo Study { get; set; } = new();

    /// <summary>Financial assumptions used in calculations.</summary>
    public FinancialAssumptions FinancialAssumptions { get; set; } = new();

    /// <summary>Calculated outputs from the reserve study.</summary>
    public CalculatedOutputs CalculatedOutputs { get; set; } = new();

    /// <summary>Company branding information.</summary>
    public BrandingInfo Branding { get; set; } = new();

    /// <summary>Report signatories (typically 1-3 professionals).</summary>
    public List<SignatoryInfo> Signatories { get; set; } = [];

    /// <summary>Vendors/contractors referenced in the study.</summary>
    public List<VendorInfo> Vendors { get; set; } = [];

    /// <summary>Glossary terms and definitions.</summary>
    public List<GlossaryTerm> GlossaryTerms { get; set; } = [];

    /// <summary>Photos to include in the report.</summary>
    public List<PhotoItem> Photos { get; set; } = [];

    /// <summary>Information furnished by management items.</summary>
    public List<InfoFurnishedItem> InfoFurnished { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════
    // COMPUTED HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Returns "City, State" formatted string.</summary>
    public string CityState => string.IsNullOrWhiteSpace(Association.City) || string.IsNullOrWhiteSpace(Association.State)
        ? Association.City ?? Association.State ?? string.Empty
        : $"{Association.City}, {Association.State}";

    /// <summary>Returns inspection date as "Month Year" (e.g., "January 2025").</summary>
    public string InspectionMonthYear => Study.InspectionDate?.ToString("MMMM yyyy") ?? string.Empty;

    /// <summary>Returns fiscal year label (e.g., "2025-2026" or "2025").</summary>
    public string FiscalYearLabel
    {
        get
        {
            if (Study.FiscalYearStart.HasValue && Study.FiscalYearEnd.HasValue)
            {
                var startYear = Study.FiscalYearStart.Value.Year;
                var endYear = Study.FiscalYearEnd.Value.Year;
                return startYear == endYear ? startYear.ToString() : $"{startYear}-{endYear}";
            }
            return Study.FiscalYearStart?.Year.ToString() ?? DateTime.Now.Year.ToString();
        }
    }

    /// <summary>Returns full address on one line.</summary>
    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Association.Address)) parts.Add(Association.Address);
            if (!string.IsNullOrWhiteSpace(CityState)) parts.Add(CityState);
            if (!string.IsNullOrWhiteSpace(Association.Zip)) parts.Add(Association.Zip);
            return string.Join(", ", parts);
        }
    }
}

/// <summary>Association/community details.</summary>
public class AssociationInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? CommunityType { get; set; } // HOA, Condo, Townhome, etc.
    public int? UnitCount { get; set; }
    public int? EstablishedYear { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

/// <summary>Study metadata and key dates.</summary>
public class StudyInfo
{
    public string? StudyType { get; set; } // Full, Update, No-Site Visit
    public string? ReportTitle { get; set; }
    public DateTime? InspectionDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? FiscalYearStart { get; set; }
    public DateTime? FiscalYearEnd { get; set; }
    public string? PreparedBy { get; set; }
    public DateTime? ReportDate { get; set; }
    public int ProjectionYears { get; set; } = 30;
}

/// <summary>Financial assumptions used in reserve calculations.</summary>
public class FinancialAssumptions
{
    public decimal InflationRate { get; set; }
    public decimal InterestRate { get; set; }
    public string? TaxTreatment { get; set; } // Tax-exempt, Taxable, etc.
    public decimal? MinimumComponentThreshold { get; set; }
    public decimal? OperatingBudgetTotal { get; set; }
    public decimal? ContingencyPercent { get; set; }
}

/// <summary>Calculated outputs from the reserve study analysis.</summary>
public class CalculatedOutputs
{
    /// <summary>Fund status label (Fully Funded, Baseline, Threshold, etc.)</summary>
    public string? FundStatusLabel { get; set; }

    /// <summary>Funding method used (Component, Cash Flow, Hybrid, etc.)</summary>
    public string? FundingMethod { get; set; }

    /// <summary>Multi-year contribution schedule.</summary>
    public List<YearContribution> ContributionSchedule { get; set; } = [];

    /// <summary>First year financial summary.</summary>
    public FirstYearSummary FirstYear { get; set; } = new();

    /// <summary>Per-unit monthly increase (optional).</summary>
    public decimal? PerUnitMonthlyIncrease { get; set; }

    /// <summary>Reserve as percent of operating budget (optional).</summary>
    public decimal? ReservePercentOfBudget { get; set; }

    /// <summary>Allocation breakdown by category (optional).</summary>
    public List<CategoryAllocation> AllocationByCategory { get; set; } = [];

    /// <summary>Percent funded at start of study.</summary>
    public decimal? PercentFunded { get; set; }

    /// <summary>Ideal/fully funded balance.</summary>
    public decimal? IdealBalance { get; set; }
}

/// <summary>Annual contribution entry.</summary>
public class YearContribution
{
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public decimal? PercentIncrease { get; set; }
    public decimal? MonthlyPerUnit { get; set; }
}

/// <summary>First year financial summary.</summary>
public class FirstYearSummary
{
    public decimal StartingBalance { get; set; }
    public decimal Contribution { get; set; }
    public decimal Interest { get; set; }
    public decimal Expenditures { get; set; }
    public decimal EndingBalance { get; set; }
}

/// <summary>Category allocation entry.</summary>
public class CategoryAllocation
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

/// <summary>Company branding information for report headers/footers.</summary>
public class BrandingInfo
{
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? FooterText { get; set; }
    public string? Tagline { get; set; }
}

/// <summary>Report signatory information.</summary>
public class SignatoryInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Credentials { get; set; }
    public string? SignatureImageUrl { get; set; }
    public DateTime? SignatureDate { get; set; }
}

/// <summary>Vendor/contractor information.</summary>
public class VendorInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; } // Roofing, Painting, Paving, etc.
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Glossary term definition.</summary>
public class GlossaryTerm
{
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}

/// <summary>Photo item for exhibits.</summary>
public class PhotoItem
{
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Information furnished by management table entry.</summary>
public class InfoFurnishedItem
{
    public string Item { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Source { get; set; }
    public int SortOrder { get; set; }
}
