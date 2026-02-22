namespace CRS.Models;

/// <summary>
/// Represents the funding status level based on percent funded thresholds.
/// Industry standard categories for reserve fund health assessment.
/// </summary>
public enum FundingStatusLevel
{
    /// <summary>
    /// 70%+ funded - Reserves are well-funded with minimal risk of special assessments.
    /// </summary>
    Strong = 0,

    /// <summary>
    /// 50-69% funded - Reserves are adequately funded but should be monitored.
    /// </summary>
    Fair = 1,

    /// <summary>
    /// 30-49% funded - Reserves are underfunded; increased contributions recommended.
    /// </summary>
    Weak = 2,

    /// <summary>
    /// Under 30% funded - Critical underfunding; special assessment risk is high.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Extension methods for FundingStatusLevel.
/// </summary>
public static class FundingStatusLevelExtensions
{
    /// <summary>
    /// Gets the funding status level based on percent funded value.
    /// </summary>
    public static FundingStatusLevel GetFundingStatus(decimal percentFunded)
    {
        return percentFunded switch
        {
            >= 70m => FundingStatusLevel.Strong,
            >= 50m => FundingStatusLevel.Fair,
            >= 30m => FundingStatusLevel.Weak,
            _ => FundingStatusLevel.Critical
        };
    }

    /// <summary>
    /// Gets the display name for the funding status level.
    /// </summary>
    public static string GetDisplayName(this FundingStatusLevel level)
    {
        return level switch
        {
            FundingStatusLevel.Strong => "Strong (70%+)",
            FundingStatusLevel.Fair => "Fair (50-69%)",
            FundingStatusLevel.Weak => "Weak (30-49%)",
            FundingStatusLevel.Critical => "Critical (<30%)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the color code for UI display (MudBlazor compatible).
    /// </summary>
    public static string GetColorClass(this FundingStatusLevel level)
    {
        return level switch
        {
            FundingStatusLevel.Strong => "success",
            FundingStatusLevel.Fair => "info",
            FundingStatusLevel.Weak => "warning",
            FundingStatusLevel.Critical => "error",
            _ => "default"
        };
    }

    /// <summary>
    /// Gets the minimum percent funded threshold for this level.
    /// </summary>
    public static decimal GetMinThreshold(this FundingStatusLevel level)
    {
        return level switch
        {
            FundingStatusLevel.Strong => 70m,
            FundingStatusLevel.Fair => 50m,
            FundingStatusLevel.Weak => 30m,
            FundingStatusLevel.Critical => 0m,
            _ => 0m
        };
    }
}
