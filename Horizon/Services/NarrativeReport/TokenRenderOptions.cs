using System.Globalization;

namespace Horizon.Services.NarrativeReport;

/// <summary>
/// Configuration options for token rendering in narrative reports.
/// </summary>
public class TokenRenderOptions
{
    /// <summary>
    /// Whether to include currency symbol in money formatting.
    /// </summary>
    public bool IncludeCurrencySymbol { get; set; } = true;

    /// <summary>
    /// Culture name for formatting (e.g., "en-US").
    /// </summary>
    public string CultureName { get; set; } = "en-US";

    /// <summary>
    /// Number of decimal places for money values.
    /// </summary>
    public int MoneyDecimals { get; set; } = 0;

    /// <summary>
    /// Number of decimal places for percent values.
    /// </summary>
    public int PercentDecimals { get; set; } = 1;

    /// <summary>
    /// Number of years to show at the beginning of the contribution schedule.
    /// </summary>
    public int ContributionScheduleYearsToShow { get; set; } = 6;

    /// <summary>
    /// If true, condenses the contribution table to show first N years, 
    /// then every 5 years, plus the final year.
    /// </summary>
    public bool CondenseContributionTable { get; set; } = true;

    /// <summary>
    /// Whether to allow image rendering (signatures, photos).
    /// </summary>
    public bool AllowImages { get; set; } = true;

    /// <summary>
    /// Maximum number of photos per row in the photo gallery.
    /// </summary>
    public int PhotosPerRow { get; set; } = 2;

    /// <summary>
    /// Gets the CultureInfo for formatting.
    /// </summary>
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);

    /// <summary>
    /// Creates default options.
    /// </summary>
    public static TokenRenderOptions Default => new();
}
