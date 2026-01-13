using Ganss.Xss;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// HTML sanitizer implementation for narrative templates using Ganss.Xss.
/// Provides robust XSS protection while allowing safe formatting tags for report content.
/// </summary>
public class NarrativeHtmlSanitizer : INarrativeHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;
    private readonly HashSet<string> _allowedTags;
    private readonly HashSet<string> _allowedAttributes;

    public NarrativeHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Configure allowed tags for narrative content
        _allowedTags =
        [
            // Text structure
            "p", "div", "span", "br", "hr",
            // Headings
            "h1", "h2", "h3", "h4", "h5", "h6",
            // Lists
            "ul", "ol", "li", "dl", "dt", "dd",
            // Tables
            "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
            // Text formatting
            "strong", "b", "em", "i", "u", "s", "del", "ins", "sub", "sup", "small", "mark",
            // Block quotes and code
            "blockquote", "pre", "code",
            // Media (limited)
            "img",
            // Links
            "a",
            // Semantic
            "article", "section", "header", "footer", "aside", "nav", "main", "figure", "figcaption",
            // Address
            "address"
        ];

        // Configure allowed attributes
        _allowedAttributes =
        [
            // Global
            "class", "id", "style", "title", "lang", "dir",
            // Table-specific
            "colspan", "rowspan", "scope", "headers",
            // Links
            "href", "rel", "target",
            // Images
            "src", "alt", "width", "height", "loading",
            // Accessibility
            "aria-label", "aria-describedby", "aria-hidden", "role", "tabindex",
            // Data attributes for styling hooks
            "data-id", "data-section", "data-block", "data-sort", "data-key"
        ];

        // Clear defaults and add our allowed tags
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in _allowedTags)
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        // Clear defaults and add our allowed attributes
        _sanitizer.AllowedAttributes.Clear();
        foreach (var attr in _allowedAttributes)
        {
            _sanitizer.AllowedAttributes.Add(attr);
        }

        // Allow data URIs for images (needed for inline images in reports)
        _sanitizer.AllowedSchemes.Add("data");

        // Allow common CSS properties for styling
        _sanitizer.AllowedCssProperties.Clear();
        var allowedCssProperties = new[]
        {
            // Text
            "color", "background-color", "background",
            "font-family", "font-size", "font-weight", "font-style",
            "text-align", "text-decoration", "text-transform", "text-indent",
            "line-height", "letter-spacing", "word-spacing",
            // Box model
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "border", "border-top", "border-right", "border-bottom", "border-left",
            "border-width", "border-style", "border-color", "border-radius",
            "width", "height", "max-width", "max-height", "min-width", "min-height",
            // Layout
            "display", "float", "clear", "position", "top", "right", "bottom", "left",
            "vertical-align", "overflow",
            // Table
            "border-collapse", "border-spacing", "table-layout",
            // Print
            "page-break-before", "page-break-after", "page-break-inside",
            "break-before", "break-after", "break-inside"
        };

        foreach (var prop in allowedCssProperties)
        {
            _sanitizer.AllowedCssProperties.Add(prop);
        }

        // Remove dangerous URL schemes
        _sanitizer.AllowedSchemes.Remove("javascript");
        _sanitizer.AllowedSchemes.Remove("vbscript");

        // Add https and http for external resources
        if (!_sanitizer.AllowedSchemes.Contains("https"))
            _sanitizer.AllowedSchemes.Add("https");
        if (!_sanitizer.AllowedSchemes.Contains("http"))
            _sanitizer.AllowedSchemes.Add("http");
    }

    public IReadOnlySet<string> AllowedTags => _allowedTags;
    public IReadOnlySet<string> AllowedAttributes => _allowedAttributes;

    /// <inheritdoc />
    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }

    /// <inheritdoc />
    public bool IsSafe(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return true;

        // Sanitize and compare - if they're equal (or very similar), it was already safe
        var sanitized = _sanitizer.Sanitize(html);
        
        // Normalize whitespace for comparison
        var normalizedOriginal = NormalizeWhitespace(html);
        var normalizedSanitized = NormalizeWhitespace(sanitized);

        return string.Equals(normalizedOriginal, normalizedSanitized, StringComparison.Ordinal);
    }

    private static string NormalizeWhitespace(string html)
    {
        // Simple normalization for comparison purposes
        return System.Text.RegularExpressions.Regex.Replace(html.Trim(), @"\s+", " ");
    }
}
