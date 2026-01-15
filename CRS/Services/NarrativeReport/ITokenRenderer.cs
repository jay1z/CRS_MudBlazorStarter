using CRS.Models.NarrativeTemplates;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Renders special tokens in narrative HTML templates to generated HTML content.
/// Supports tokens like [[PAGE_BREAK]], [[TABLE:ContributionSchedule]], [[SIGNATURES]], etc.
/// </summary>
public interface ITokenRenderer
{
    /// <summary>
    /// Renders a single token to its HTML representation.
    /// </summary>
    /// <param name="tokenName">The token name (e.g., "PAGE_BREAK", "TABLE:ContributionSchedule").</param>
    /// <param name="context">The report context containing all data for rendering.</param>
    /// <param name="options">Rendering options for formatting and display.</param>
    /// <returns>The generated HTML content for the token.</returns>
    string RenderToken(string tokenName, ReserveStudyReportContext context, TokenRenderOptions options);

    /// <summary>
    /// Replaces all tokens in the given HTML with their rendered content.
    /// </summary>
    /// <param name="html">The HTML containing tokens in [[TOKEN]] or [[TOKEN:Param]] format.</param>
    /// <param name="context">The report context containing all data for rendering.</param>
    /// <param name="options">Rendering options for formatting and display.</param>
    /// <returns>The HTML with all tokens replaced.</returns>
    string ReplaceAllTokens(string html, ReserveStudyReportContext context, TokenRenderOptions? options = null);

    /// <summary>
    /// Gets all supported token names.
    /// </summary>
    /// <returns>Collection of supported token names.</returns>
    IEnumerable<string> GetSupportedTokens();
}
