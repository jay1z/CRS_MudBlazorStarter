namespace CRS.Services.NarrativeReport;

/// <summary>
/// Sanitizes HTML content to prevent XSS and other security issues.
/// Designed for user-authored HTML templates.
/// </summary>
public interface INarrativeHtmlSanitizer
{
    /// <summary>
    /// Sanitizes HTML content by removing unsafe elements and attributes.
    /// </summary>
    /// <param name="html">The HTML content to sanitize.</param>
    /// <returns>Sanitized HTML content.</returns>
    string Sanitize(string html);

    /// <summary>
    /// Checks if HTML content contains any unsafe elements.
    /// </summary>
    /// <param name="html">The HTML content to check.</param>
    /// <returns>True if the HTML is safe, false otherwise.</returns>
    bool IsSafe(string html);

    /// <summary>
    /// Gets the list of allowed HTML tags.
    /// </summary>
    IReadOnlySet<string> AllowedTags { get; }

    /// <summary>
    /// Gets the list of allowed HTML attributes.
    /// </summary>
    IReadOnlySet<string> AllowedAttributes { get; }
}
