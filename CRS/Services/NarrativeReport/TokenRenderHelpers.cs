using System.Globalization;
using System.Web;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Helper methods for formatting and encoding values in token rendering.
/// </summary>
public static class TokenRenderHelpers
{
    /// <summary>
    /// HTML-encodes a string to prevent XSS and injection.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>HTML-encoded string.</returns>
    public static string HtmlEncode(string? text)
    {
        return HttpUtility.HtmlEncode(text ?? string.Empty);
    }

    /// <summary>
    /// Formats a decimal value as money.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="options">Rendering options for culture and formatting.</param>
    /// <returns>Formatted money string.</returns>
    public static string Money(decimal? value, TokenRenderOptions options)
    {
        if (!value.HasValue) return string.Empty;

        var culture = options.Culture;
        
        if (options.IncludeCurrencySymbol)
        {
            var format = options.MoneyDecimals == 0 ? "C0" : $"C{options.MoneyDecimals}";
            return value.Value.ToString(format, culture);
        }
        else
        {
            var format = options.MoneyDecimals == 0 ? "N0" : $"N{options.MoneyDecimals}";
            return value.Value.ToString(format, culture);
        }
    }

    /// <summary>
    /// Formats a decimal value as a percentage.
    /// </summary>
    /// <param name="value">The value to format (0.03 = 3%).</param>
    /// <param name="options">Rendering options for culture and formatting.</param>
    /// <param name="isAlreadyPercentage">True if value is already in percentage form (3 instead of 0.03).</param>
    /// <returns>Formatted percentage string.</returns>
    public static string Percent(decimal? value, TokenRenderOptions options, bool isAlreadyPercentage = false)
    {
        if (!value.HasValue) return string.Empty;

        var percentage = isAlreadyPercentage ? value.Value : value.Value * 100;
        var format = $"F{options.PercentDecimals}";
        return $"{percentage.ToString(format, options.Culture)}%";
    }

    /// <summary>
    /// Formats a date value.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <param name="format">The date format string (default: "MMMM d, yyyy").</param>
    /// <param name="culture">The culture for formatting.</param>
    /// <returns>Formatted date string.</returns>
    public static string Date(DateTime? date, string format = "MMMM d, yyyy", CultureInfo? culture = null)
    {
        if (!date.HasValue) return string.Empty;
        return date.Value.ToString(format, culture ?? CultureInfo.GetCultureInfo("en-US"));
    }

    /// <summary>
    /// Formats a date value using rendering options.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <param name="options">Rendering options for culture.</param>
    /// <param name="format">The date format string (default: "MMMM d, yyyy").</param>
    /// <returns>Formatted date string.</returns>
    public static string Date(DateTime? date, TokenRenderOptions options, string format = "MMMM d, yyyy")
    {
        return Date(date, format, options.Culture);
    }

    /// <summary>
    /// Joins non-empty strings with a separator.
    /// </summary>
    /// <param name="separator">The separator to use.</param>
    /// <param name="values">The values to join.</param>
    /// <returns>Joined string with only non-empty values.</returns>
    public static string JoinNonEmpty(string separator, params string?[] values)
    {
        return string.Join(separator, values.Where(v => !string.IsNullOrWhiteSpace(v)));
    }
}
