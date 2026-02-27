using Horizon.Models.NarrativeTemplates;

namespace Horizon.Services.NarrativeReport;

/// <summary>
/// Resolves placeholders in narrative templates to actual values from the report context.
/// </summary>
public interface IPlaceholderResolver
{
    /// <summary>
    /// Resolves all placeholders to their values.
    /// </summary>
    /// <param name="context">The report context containing all data.</param>
    /// <returns>Dictionary mapping placeholder names to resolved values.</returns>
    Dictionary<string, string> Resolve(ReserveStudyReportContext context);

    /// <summary>
    /// Replaces placeholders in HTML template with resolved values.
    /// </summary>
    /// <param name="htmlTemplate">The HTML template with {Placeholder} syntax.</param>
    /// <param name="context">The report context containing all data.</param>
    /// <returns>HTML with placeholders replaced.</returns>
    string ReplacePlaceholders(string htmlTemplate, ReserveStudyReportContext context);

    /// <summary>
    /// Replaces special tokens with injected HTML content.
    /// Tokens: [[PAGE_BREAK]], [[TABLE:ContributionSchedule]], [[SIGNATURES]], [[PHOTOS]], etc.
    /// </summary>
    /// <param name="html">The HTML content with tokens.</param>
    /// <param name="context">The report context containing all data.</param>
    /// <returns>HTML with tokens replaced by generated content.</returns>
    string ReplaceTokens(string html, ReserveStudyReportContext context);
}
