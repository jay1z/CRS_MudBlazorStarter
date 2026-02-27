using Horizon.Models.NarrativeTemplates;

namespace Horizon.Services.NarrativeReport;

/// <summary>
/// Composes a complete HTML document from narrative templates for a reserve study report.
/// </summary>
public interface IReserveStudyHtmlComposer
{
    /// <summary>
    /// Composes the full HTML document for a reserve study narrative report.
    /// </summary>
    /// <param name="context">The report context with all data.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific templates (null uses global defaults).</param>
    /// <param name="options">Optional composition options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete HTML document as a string.</returns>
    Task<string> ComposeAsync(
        ReserveStudyReportContext context,
        int? tenantId = null,
        NarrativeCompositionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Composes a single section of the narrative.
    /// </summary>
    /// <param name="sectionKey">The section key to render.</param>
    /// <param name="context">The report context with all data.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML fragment for the section.</returns>
    Task<string> ComposeSectionAsync(
        string sectionKey,
        ReserveStudyReportContext context,
        int? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the print-friendly CSS for PDF rendering.
    /// </summary>
    /// <returns>CSS content as a string.</returns>
    string GetPrintCss();
}

/// <summary>
/// Options for narrative composition.
/// </summary>
public class NarrativeCompositionOptions
{
    /// <summary>
    /// Whether to include the full HTML document wrapper (html, head, body tags).
    /// Default is true.
    /// </summary>
    public bool IncludeDocumentWrapper { get; set; } = true;

    /// <summary>
    /// Whether to include print CSS in the document.
    /// Default is true.
    /// </summary>
    public bool IncludePrintCss { get; set; } = true;

    /// <summary>
    /// Whether to include page breaks between major sections.
    /// Default is true.
    /// </summary>
    public bool IncludePageBreaks { get; set; } = true;

    /// <summary>
    /// Optional custom CSS to include.
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Section keys to exclude from rendering.
    /// </summary>
    public HashSet<string>? ExcludedSections { get; set; }

    /// <summary>
    /// If true, only render sections in the IncludedSections set.
    /// </summary>
    public bool OnlyIncludedSections { get; set; } = false;

    /// <summary>
    /// Section keys to explicitly include (used with OnlyIncludedSections).
    /// </summary>
    public HashSet<string>? IncludedSections { get; set; }

    /// <summary>
    /// Whether to render a preview (may skip some processing).
    /// Default is false.
    /// </summary>
    public bool IsPreview { get; set; } = false;
}
