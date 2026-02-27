using System.Text;
using System.Text.RegularExpressions;

using Horizon.Models.NarrativeTemplates;

using static Horizon.Services.NarrativeReport.TokenRenderHelpers;

namespace Horizon.Services.NarrativeReport;

/// <summary>
/// Default implementation of <see cref="ITokenRenderer"/>.
/// Renders special tokens in narrative HTML templates to generated HTML content.
/// </summary>
public class DefaultTokenRenderer : ITokenRenderer
{
    private readonly ILogger<DefaultTokenRenderer> _logger;

    // Regex for [[TOKEN:Param]] or [[TOKEN]] syntax
    private static readonly Regex TokenRegex = new(@"\[\[([A-Z_]+)(?::([A-Za-z0-9_]+))?\]\]", RegexOptions.Compiled);

    /// <summary>
    /// Supported token names.
    /// </summary>
    private static readonly HashSet<string> SupportedTokenNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PAGE_BREAK",
        "TABLE:ContributionSchedule",
        "TABLE:InfoFurnished",
        "SIGNATURES",
        "PHOTOS",
        "VENDORS",
        "GLOSSARY"
    };

    public DefaultTokenRenderer(ILogger<DefaultTokenRenderer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedTokens() => SupportedTokenNames;

    /// <inheritdoc />
    public string ReplaceAllTokens(string html, ReserveStudyReportContext context, TokenRenderOptions? options = null)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        options ??= TokenRenderOptions.Default;

        return TokenRegex.Replace(html, match =>
        {
            var token = match.Groups[1].Value;
            var param = match.Groups[2].Success ? match.Groups[2].Value : null;

            // Build full token name for parameterized tokens
            var tokenName = string.IsNullOrEmpty(param) ? token : $"{token}:{param}";

            try
            {
                return RenderToken(tokenName, context, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering token {Token}", tokenName);
                return string.Empty;
            }
        });
    }

    /// <inheritdoc />
    public string RenderToken(string tokenName, ReserveStudyReportContext context, TokenRenderOptions options)
    {
        return tokenName.ToUpperInvariant() switch
        {
            "PAGE_BREAK" => RenderPageBreak(),
            "TABLE:CONTRIBUTIONSCHEDULE" => RenderContributionScheduleTable(context, options),
            "TABLE:INFOFURNISHED" => RenderInfoFurnishedTable(context, options),
            "SIGNATURES" => RenderSignatures(context, options),
            "PHOTOS" => RenderPhotos(context, options),
            "VENDORS" => RenderVendors(context, options),
            "GLOSSARY" => RenderGlossary(context, options),
            _ => HandleUnknownToken(tokenName)
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE BREAK
    // ═══════════════════════════════════════════════════════════════

    private static string RenderPageBreak()
    {
        return "<div class=\"page-break\"></div>";
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTRIBUTION SCHEDULE TABLE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the contribution schedule table.
    /// If CondenseContributionTable is true, shows first N years, then every 5 years, plus final year.
    /// </summary>
    private static string RenderContributionScheduleTable(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        var schedule = context.CalculatedOutputs.ContributionSchedule;
        if (schedule.Count == 0)
        {
            return "<p><em>No contribution schedule available.</em></p>";
        }

        var yearsToShow = GetCondensedYears(schedule, options);

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Year</th>");
        sb.AppendLine("<th>Recommended Reserve Contribution</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var year in yearsToShow)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{year.Year}</td>");
            sb.AppendLine($"<td>{Money(year.Amount, options)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>
    /// Gets the years to display in a condensed contribution schedule.
    /// </summary>
    private static List<YearContribution> GetCondensedYears(List<YearContribution> schedule, TokenRenderOptions options)
    {
        if (!options.CondenseContributionTable || schedule.Count <= options.ContributionScheduleYearsToShow)
        {
            return schedule;
        }

        var result = new List<YearContribution>();
        var firstN = options.ContributionScheduleYearsToShow;

        // Add first N years
        result.AddRange(schedule.Take(firstN));

        if (schedule.Count > firstN)
        {
            var remaining = schedule.Skip(firstN).ToList();
            var lastYear = schedule.Last();

            // Add every 5th year from the remaining years
            var lastYearShown = result.Last().Year;
            foreach (var yearData in remaining)
            {
                // Show every 5 years (e.g., year 10, 15, 20, 25, 30)
                if ((yearData.Year - lastYearShown) >= 5 && yearData.Year != lastYear.Year)
                {
                    result.Add(yearData);
                    lastYearShown = yearData.Year;
                }
            }

            // Always include the final year if not already included
            if (result.Last().Year != lastYear.Year)
            {
                result.Add(lastYear);
            }
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // INFORMATION FURNISHED TABLE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the Information Furnished by Management table.
    /// Shows first-year financial summary in a two-column key/value format.
    /// </summary>
    private static string RenderInfoFurnishedTable(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        var firstYear = context.CalculatedOutputs.FirstYear;
        var startDate = context.Study.FiscalYearStart ?? context.Study.EffectiveDate;
        var startDateLabel = startDate.HasValue 
            ? $" (as of {Date(startDate, options, "MMMM d, yyyy")})" 
            : string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<tbody>");

        // Starting Reserve Cash Balance
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td>Starting Reserve Cash Balance{HtmlEncode(startDateLabel)}</td>");
        sb.AppendLine($"<td>{Money(firstYear.StartingBalance, options)}</td>");
        sb.AppendLine("</tr>");

        // Budgeted Reserve Contributions
        sb.AppendLine("<tr>");
        sb.AppendLine("<td>Budgeted Reserve Contributions</td>");
        sb.AppendLine($"<td>{Money(firstYear.Contribution, options)}</td>");
        sb.AppendLine("</tr>");

        // Anticipated Interest Earned
        sb.AppendLine("<tr>");
        sb.AppendLine("<td>Anticipated Interest Earned</td>");
        sb.AppendLine($"<td>{Money(firstYear.Interest, options)}</td>");
        sb.AppendLine("</tr>");

        // Less: Anticipated Expenditures (displayed as positive, label indicates subtraction)
        sb.AppendLine("<tr>");
        sb.AppendLine("<td>Less: Anticipated Expenditures</td>");
        sb.AppendLine($"<td>{Money(firstYear.Expenditures, options)}</td>");
        sb.AppendLine("</tr>");

        // Anticipated Year-End Reserve Cash Balance
        sb.AppendLine("<tr class=\"total-row\">");
        sb.AppendLine("<td><strong>Anticipated Year-End Reserve Cash Balance</strong></td>");
        sb.AppendLine($"<td><strong>{Money(firstYear.EndingBalance, options)}</strong></td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // SIGNATURES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the signature block for all signatories.
    /// </summary>
    private static string RenderSignatures(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        if (context.Signatories.Count == 0)
        {
            return "<p><em>No signatories defined.</em></p>";
        }

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"signature-container\">");

        foreach (var signatory in context.Signatories)
        {
            sb.AppendLine("<div class=\"signature-block\" style=\"margin-bottom: 2em;\">");

            // Signature image (optional, if AllowImages)
            if (options.AllowImages && !string.IsNullOrEmpty(signatory.SignatureImageUrl))
            {
                sb.AppendLine($"<img src=\"{HtmlEncode(signatory.SignatureImageUrl)}\" alt=\"Signature\" style=\"max-width: 200px; max-height: 60px;\" />");
            }
            else
            {
                // Signature line placeholder
                sb.AppendLine("<div style=\"border-bottom: 1px solid #000; width: 250px; margin-bottom: 0.5em;\"></div>");
            }

            // Name (bold)
            sb.AppendLine($"<p style=\"margin: 0.25em 0;\"><strong>{HtmlEncode(signatory.Name)}</strong></p>");

            // Title and credentials
            var titleLine = JoinNonEmpty(", ", signatory.Title, signatory.Credentials);
            if (!string.IsNullOrEmpty(titleLine))
            {
                sb.AppendLine($"<p style=\"margin: 0.25em 0;\">{HtmlEncode(titleLine)}</p>");
            }

            // License number (optional) - check if signatory has it via reflection or add to model
            // Note: Current model doesn't have LicenseNumber, but the spec mentions it
            // We'll handle this gracefully if the property exists

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // PHOTOS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the photo gallery using a table layout for PDF compatibility.
    /// </summary>
    private static string RenderPhotos(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        if (!options.AllowImages || context.Photos.Count == 0)
        {
            return "<p><em>No photos available.</em></p>";
        }

        var photos = context.Photos.OrderBy(p => p.SortOrder).ToList();
        var photosPerRow = options.PhotosPerRow;

        var sb = new StringBuilder();
        sb.AppendLine("<table style=\"width:100%; border-collapse:collapse;\">");

        for (var i = 0; i < photos.Count; i += photosPerRow)
        {
            sb.AppendLine("<tr>");

            for (var j = 0; j < photosPerRow; j++)
            {
                var idx = i + j;
                if (idx < photos.Count)
                {
                    var photo = photos[idx];
                    sb.AppendLine("<td style=\"padding: 0.5em; vertical-align: top; width: 50%;\">");
                    sb.AppendLine("<div class=\"photo-item\">");
                    sb.AppendLine($"<img src=\"{HtmlEncode(photo.Url)}\" alt=\"{HtmlEncode(photo.Caption ?? "Site Photo")}\" style=\"max-width:100%; height:auto;\" />");
                    
                    if (!string.IsNullOrEmpty(photo.Caption))
                    {
                        sb.AppendLine($"<p class=\"small\" style=\"margin: 0.25em 0;\">{HtmlEncode(photo.Caption)}</p>");
                    }
                    
                    sb.AppendLine("</div>");
                    sb.AppendLine("</td>");
                }
                else
                {
                    // Empty cell for alignment
                    sb.AppendLine("<td></td>");
                }
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // VENDORS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the vendor list table.
    /// </summary>
    private static string RenderVendors(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        if (context.Vendors.Count == 0)
        {
            return string.Empty; // Empty string per spec for no vendors
        }

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Vendor</th>");
        sb.AppendLine("<th>Service</th>");
        sb.AppendLine("<th>Contact</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var vendor in context.Vendors)
        {
            // Build contact string from phone/email/website
            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(vendor.Phone))
                contactParts.Add(vendor.Phone);
            if (!string.IsNullOrWhiteSpace(vendor.Email))
                contactParts.Add(vendor.Email);
            if (!string.IsNullOrWhiteSpace(vendor.Website))
                contactParts.Add(vendor.Website);

            var contact = contactParts.Count > 0 ? string.Join(" | ", contactParts) : "-";

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HtmlEncode(vendor.Name)}</td>");
            sb.AppendLine($"<td>{HtmlEncode(vendor.Category ?? "-")}</td>");
            sb.AppendLine($"<td>{HtmlEncode(contact)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // GLOSSARY
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the glossary table with Term and Definition columns.
    /// </summary>
    private static string RenderGlossary(ReserveStudyReportContext context, TokenRenderOptions options)
    {
        if (context.GlossaryTerms.Count == 0)
        {
            return string.Empty; // Empty string per spec for no glossary
        }

        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th style=\"width: 25%;\">Term</th>");
        sb.AppendLine("<th style=\"width: 75%;\">Definition</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var term in context.GlossaryTerms.OrderBy(t => t.Term))
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HtmlEncode(term.Term)}</td>");
            sb.AppendLine($"<td>{HtmlEncode(term.Definition)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // UNKNOWN TOKEN HANDLING
    // ═══════════════════════════════════════════════════════════════

    private string HandleUnknownToken(string tokenName)
    {
        _logger.LogWarning("Unknown token encountered: {Token}", tokenName);
        return string.Empty; // Replace unknown tokens with empty string per spec
    }
}
