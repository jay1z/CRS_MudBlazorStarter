using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using CRS.Models.NarrativeTemplates;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Resolves placeholders and tokens in narrative HTML templates.
/// </summary>
public partial class PlaceholderResolver : IPlaceholderResolver
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    // Regex for {PlaceholderName} syntax
    [GeneratedRegex(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    // Regex for [[TOKEN:Param]] or [[TOKEN]] syntax
    [GeneratedRegex(@"\[\[([A-Z_]+)(?::([A-Za-z0-9_]+))?\]\]", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    /// <inheritdoc />
    public Dictionary<string, string> Resolve(ReserveStudyReportContext context)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Association Info
        dict["AssociationName"] = context.Association.Name ?? string.Empty;
        dict["AssociationAddress"] = context.Association.Address ?? string.Empty;
        dict["AssociationCity"] = context.Association.City ?? string.Empty;
        dict["AssociationState"] = context.Association.State ?? string.Empty;
        dict["AssociationZip"] = context.Association.Zip ?? string.Empty;
        dict["CityState"] = context.CityState;
        dict["FullAddress"] = context.FullAddress;
        dict["CommunityType"] = context.Association.CommunityType ?? string.Empty;
        dict["UnitCount"] = context.Association.UnitCount?.ToString() ?? string.Empty;
        dict["EstablishedYear"] = context.Association.EstablishedYear?.ToString() ?? string.Empty;
        dict["AssociationPhone"] = context.Association.PhoneNumber ?? string.Empty;
        dict["AssociationEmail"] = context.Association.Email ?? string.Empty;

        // Study Info
        dict["StudyType"] = context.Study.StudyType ?? string.Empty;
        dict["ReportTitle"] = context.Study.ReportTitle ?? string.Empty;
        dict["InspectionDate"] = FormatDate(context.Study.InspectionDate);
        dict["InspectionMonthYear"] = context.InspectionMonthYear;
        dict["EffectiveDate"] = FormatDate(context.Study.EffectiveDate);
        dict["FiscalYearStart"] = FormatDate(context.Study.FiscalYearStart);
        dict["FiscalYearEnd"] = FormatDate(context.Study.FiscalYearEnd);
        dict["FiscalYearLabel"] = context.FiscalYearLabel;
        dict["PreparedBy"] = context.Study.PreparedBy ?? string.Empty;
        dict["ReportDate"] = FormatDate(context.Study.ReportDate ?? DateTime.Now);
        dict["ProjectionYears"] = context.Study.ProjectionYears.ToString();

        // Financial Assumptions
        dict["InflationRate"] = FormatPercent(context.FinancialAssumptions.InflationRate);
        dict["InterestRate"] = FormatPercent(context.FinancialAssumptions.InterestRate);
        dict["TaxTreatment"] = context.FinancialAssumptions.TaxTreatment ?? string.Empty;
        dict["MinimumComponentThreshold"] = FormatMoney(context.FinancialAssumptions.MinimumComponentThreshold);
        dict["OperatingBudgetTotal"] = FormatMoney(context.FinancialAssumptions.OperatingBudgetTotal);
        dict["ContingencyPercent"] = FormatPercent(context.FinancialAssumptions.ContingencyPercent ?? 0);

        // Calculated Outputs
        dict["FundStatusLabel"] = context.CalculatedOutputs.FundStatusLabel ?? string.Empty;
        dict["FundingMethod"] = context.CalculatedOutputs.FundingMethod ?? string.Empty;
        dict["PercentFunded"] = FormatPercent(context.CalculatedOutputs.PercentFunded ?? 0, decimalPlaces: 0);
        dict["IdealBalance"] = FormatMoney(context.CalculatedOutputs.IdealBalance);
        dict["PerUnitMonthlyIncrease"] = FormatMoney(context.CalculatedOutputs.PerUnitMonthlyIncrease);
        dict["ReservePercentOfBudget"] = FormatPercent(context.CalculatedOutputs.ReservePercentOfBudget ?? 0);

        // First Year Summary
        dict["StartingBalance"] = FormatMoney(context.CalculatedOutputs.FirstYear.StartingBalance);
        dict["StartingReserveBalance"] = FormatMoney(context.CalculatedOutputs.FirstYear.StartingBalance);
        dict["FirstYearContribution"] = FormatMoney(context.CalculatedOutputs.FirstYear.Contribution);
        dict["FirstYearInterest"] = FormatMoney(context.CalculatedOutputs.FirstYear.Interest);
        dict["FirstYearExpenditures"] = FormatMoney(context.CalculatedOutputs.FirstYear.Expenditures);
        dict["EndingBalance"] = FormatMoney(context.CalculatedOutputs.FirstYear.EndingBalance);

        // Branding
        dict["CompanyName"] = context.Branding.CompanyName ?? string.Empty;
        dict["CompanyPhone"] = context.Branding.Phone ?? string.Empty;
        dict["CompanyEmail"] = context.Branding.Email ?? string.Empty;
        dict["CompanyWebsite"] = context.Branding.Website ?? string.Empty;
        dict["CompanyAddress"] = context.Branding.Address ?? string.Empty;
        dict["LogoUrl"] = context.Branding.LogoUrl ?? string.Empty;
        dict["CoverImageUrl"] = context.Branding.CoverImageUrl ?? string.Empty;
        dict["FooterText"] = context.Branding.FooterText ?? string.Empty;
        dict["Tagline"] = context.Branding.Tagline ?? string.Empty;

        // Current date helpers
        dict["CurrentDate"] = FormatDate(DateTime.Now);
        dict["CurrentYear"] = DateTime.Now.Year.ToString();

        return dict;
    }

    /// <inheritdoc />
    public string ReplacePlaceholders(string htmlTemplate, ReserveStudyReportContext context)
    {
        if (string.IsNullOrEmpty(htmlTemplate)) return string.Empty;

        var placeholders = Resolve(context);

        return PlaceholderRegex().Replace(htmlTemplate, match =>
        {
            var placeholderName = match.Groups[1].Value;
            if (placeholders.TryGetValue(placeholderName, out var value))
            {
                // HTML encode the value to prevent XSS, except for URLs
                if (placeholderName.EndsWith("Url", StringComparison.OrdinalIgnoreCase) ||
                    placeholderName.EndsWith("Image", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
                return HttpUtility.HtmlEncode(value);
            }
            // Return empty string for missing placeholders (graceful handling)
            return string.Empty;
        });
    }

    /// <inheritdoc />
    public string ReplaceTokens(string html, ReserveStudyReportContext context)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        return TokenRegex().Replace(html, match =>
        {
            var token = match.Groups[1].Value;
            var param = match.Groups[2].Success ? match.Groups[2].Value : null;

            return token switch
            {
                "PAGE_BREAK" => GeneratePageBreak(),
                "TABLE" => GenerateTable(param, context),
                "SIGNATURES" => GenerateSignatures(context),
                "PHOTOS" => GeneratePhotoGallery(context),
                "VENDORS" => GenerateVendorList(context),
                "GLOSSARY" => GenerateGlossary(context),
                _ => string.Empty // Unknown token
            };
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORMATTING HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static string FormatMoney(decimal? value)
    {
        if (!value.HasValue) return string.Empty;
        return value.Value.ToString("C", UsCulture);
    }

    private static string FormatPercent(decimal? value, int decimalPlaces = 1)
    {
        if (!value.HasValue) return string.Empty;
        // Assume value is in decimal form (0.03 = 3%)
        var percentage = value.Value * 100;
        return $"{percentage.ToString($"F{decimalPlaces}", UsCulture)}%";
    }

    private static string FormatDate(DateTime? date, string format = "MMMM d, yyyy")
    {
        return date?.ToString(format, UsCulture) ?? string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    // TOKEN GENERATORS
    // ═══════════════════════════════════════════════════════════════

    private static string GeneratePageBreak()
    {
        return "<div class=\"page-break\"></div>";
    }

    private static string GenerateTable(string? tableType, ReserveStudyReportContext context)
    {
        return tableType switch
        {
            "ContributionSchedule" => GenerateContributionScheduleTable(context),
            "InfoFurnished" => GenerateInfoFurnishedTable(context),
            "AllocationByCategory" => GenerateAllocationTable(context),
            _ => string.Empty
        };
    }

    private static string GenerateContributionScheduleTable(ReserveStudyReportContext context)
    {
        if (context.CalculatedOutputs.ContributionSchedule.Count == 0)
            return "<p><em>No contribution schedule available.</em></p>";

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"contribution-schedule\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Year</th>");
        sb.AppendLine("<th>Annual Contribution</th>");
        if (context.CalculatedOutputs.ContributionSchedule.Any(c => c.MonthlyPerUnit.HasValue))
            sb.AppendLine("<th>Monthly/Unit</th>");
        if (context.CalculatedOutputs.ContributionSchedule.Any(c => c.PercentIncrease.HasValue))
            sb.AppendLine("<th>% Increase</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var year in context.CalculatedOutputs.ContributionSchedule)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{year.Year}</td>");
            sb.AppendLine($"<td>{FormatMoney(year.Amount)}</td>");
            if (context.CalculatedOutputs.ContributionSchedule.Any(c => c.MonthlyPerUnit.HasValue))
                sb.AppendLine($"<td>{FormatMoney(year.MonthlyPerUnit)}</td>");
            if (context.CalculatedOutputs.ContributionSchedule.Any(c => c.PercentIncrease.HasValue))
                sb.AppendLine($"<td>{FormatPercent(year.PercentIncrease)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string GenerateInfoFurnishedTable(ReserveStudyReportContext context)
    {
        if (context.InfoFurnished.Count == 0)
            return "<p><em>No information furnished data available.</em></p>";

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"info-furnished\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Item</th>");
        sb.AppendLine("<th>Value</th>");
        sb.AppendLine("<th>Source</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var item in context.InfoFurnished.OrderBy(i => i.SortOrder))
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(item.Item)}</td>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(item.Value ?? "-")}</td>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(item.Source ?? "-")}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string GenerateAllocationTable(ReserveStudyReportContext context)
    {
        if (context.CalculatedOutputs.AllocationByCategory.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"allocation-table\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Category</th>");
        sb.AppendLine("<th>Amount</th>");
        sb.AppendLine("<th>Percent</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var cat in context.CalculatedOutputs.AllocationByCategory)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(cat.Category)}</td>");
            sb.AppendLine($"<td>{FormatMoney(cat.Amount)}</td>");
            sb.AppendLine($"<td>{FormatPercent(cat.Percent)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string GenerateSignatures(ReserveStudyReportContext context)
    {
        if (context.Signatories.Count == 0)
            return "<p><em>No signatories defined.</em></p>";

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"signature-container\">");

        foreach (var sig in context.Signatories)
        {
            sb.AppendLine("<div class=\"signature-block\">");
            if (!string.IsNullOrEmpty(sig.SignatureImageUrl))
            {
                sb.AppendLine($"<img src=\"{sig.SignatureImageUrl}\" alt=\"Signature\" class=\"signature-image\" />");
            }
            else
            {
                sb.AppendLine("<div class=\"signature-line\"></div>");
            }
            sb.AppendLine($"<p class=\"signature-name\">{HttpUtility.HtmlEncode(sig.Name)}</p>");
            if (!string.IsNullOrEmpty(sig.Title))
                sb.AppendLine($"<p class=\"signature-title\">{HttpUtility.HtmlEncode(sig.Title)}</p>");
            if (!string.IsNullOrEmpty(sig.Credentials))
                sb.AppendLine($"<p class=\"signature-credentials\">{HttpUtility.HtmlEncode(sig.Credentials)}</p>");
            if (sig.SignatureDate.HasValue)
                sb.AppendLine($"<p class=\"signature-date\">{FormatDate(sig.SignatureDate)}</p>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string GeneratePhotoGallery(ReserveStudyReportContext context)
    {
        if (context.Photos.Count == 0)
            return "<p><em>No photos available.</em></p>";

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"photo-gallery\">");

        foreach (var photo in context.Photos.OrderBy(p => p.SortOrder))
        {
            sb.AppendLine("<div class=\"photo-item\">");
            sb.AppendLine($"<img src=\"{photo.Url}\" alt=\"{HttpUtility.HtmlEncode(photo.Caption ?? "Site Photo")}\" class=\"photo-image\" />");
            if (!string.IsNullOrEmpty(photo.Caption))
                sb.AppendLine($"<p class=\"photo-caption\">{HttpUtility.HtmlEncode(photo.Caption)}</p>");
            if (!string.IsNullOrEmpty(photo.Category))
                sb.AppendLine($"<p class=\"photo-category\">{HttpUtility.HtmlEncode(photo.Category)}</p>");
            if (!string.IsNullOrEmpty(photo.Condition))
                sb.AppendLine($"<p class=\"photo-condition\">Condition: {HttpUtility.HtmlEncode(photo.Condition)}</p>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string GenerateVendorList(ReserveStudyReportContext context)
    {
        if (context.Vendors.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"vendor-list\">");
        sb.AppendLine("<h3>Vendors &amp; Service Providers</h3>");
        sb.AppendLine("<table class=\"vendor-table\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Name</th>");
        sb.AppendLine("<th>Category</th>");
        sb.AppendLine("<th>Contact</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var vendor in context.Vendors)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(vendor.Name)}</td>");
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(vendor.Category ?? "-")}</td>");
            var contact = vendor.Phone ?? vendor.Email ?? "-";
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(contact)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string GenerateGlossary(ReserveStudyReportContext context)
    {
        if (context.GlossaryTerms.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"glossary\">");
        sb.AppendLine("<h3>Glossary of Terms</h3>");
        sb.AppendLine("<dl class=\"glossary-list\">");

        foreach (var term in context.GlossaryTerms.OrderBy(t => t.Term))
        {
            sb.AppendLine($"<dt>{HttpUtility.HtmlEncode(term.Term)}</dt>");
            sb.AppendLine($"<dd>{HttpUtility.HtmlEncode(term.Definition)}</dd>");
        }

        sb.AppendLine("</dl>");
        sb.AppendLine("</div>");
        return sb.ToString();
    }
}
