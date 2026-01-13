namespace CRS.Services.NarrativeReport;

/// <summary>
/// Engine-agnostic interface for converting HTML to PDF.
/// Implementations can use different PDF engines (Puppeteer, wkhtmltopdf, iText, etc.).
/// </summary>
public interface IPdfConverter
{
    /// <summary>
    /// Converts HTML content to a PDF document.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <param name="options">Optional PDF conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF document as a byte array.</returns>
    Task<byte[]> ConvertToPdfAsync(
        string html,
        PdfConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the PDF converter is available and properly configured.
    /// </summary>
    /// <returns>True if the converter is ready to use.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of the PDF engine being used.
    /// </summary>
    string EngineName { get; }
}

/// <summary>
/// Options for PDF conversion.
/// </summary>
public class PdfConversionOptions
{
    /// <summary>
    /// Page size. Default is "Letter" (8.5" x 11").
    /// </summary>
    public string PageSize { get; set; } = "Letter";

    /// <summary>
    /// Page orientation. Default is Portrait.
    /// </summary>
    public PdfOrientation Orientation { get; set; } = PdfOrientation.Portrait;

    /// <summary>
    /// Top margin in inches.
    /// </summary>
    public double MarginTop { get; set; } = 0.75;

    /// <summary>
    /// Bottom margin in inches.
    /// </summary>
    public double MarginBottom { get; set; } = 1.0;

    /// <summary>
    /// Left margin in inches.
    /// </summary>
    public double MarginLeft { get; set; } = 0.75;

    /// <summary>
    /// Right margin in inches.
    /// </summary>
    public double MarginRight { get; set; } = 0.75;

    /// <summary>
    /// HTML content for the page header.
    /// </summary>
    public string? HeaderHtml { get; set; }

    /// <summary>
    /// HTML content for the page footer.
    /// </summary>
    public string? FooterHtml { get; set; }

    /// <summary>
    /// Whether to display page numbers. Default is true.
    /// </summary>
    public bool ShowPageNumbers { get; set; } = true;

    /// <summary>
    /// Starting page number. Default is 1.
    /// </summary>
    public int StartingPageNumber { get; set; } = 1;

    /// <summary>
    /// Optional title for the PDF document metadata.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional author for the PDF document metadata.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Optional subject for the PDF document metadata.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Print background colors and images. Default is true.
    /// </summary>
    public bool PrintBackground { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for PDF generation. Default is 60.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// PDF page orientation.
/// </summary>
public enum PdfOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Placeholder PDF converter that returns an error or placeholder PDF.
/// Use this when no PDF engine is configured.
/// </summary>
public class NullPdfConverter : IPdfConverter
{
    public string EngineName => "None (Placeholder)";

    public Task<byte[]> ConvertToPdfAsync(
        string html,
        PdfConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "No PDF converter is configured. " +
            "Implement IPdfConverter with your preferred PDF engine (e.g., Puppeteer, wkhtmltopdf, iText).");
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
