namespace CRS.Core.ReserveCalculator;

/// <summary>
/// Provides deterministic money calculations with consistent rounding.
/// All monetary values should use decimal to avoid floating-point precision issues.
/// </summary>
public static class Money
{
    /// <summary>
    /// Rounds a decimal value to 2 decimal places using banker's rounding (MidpointRounding.ToEven).
    /// This is the standard for financial calculations.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The value rounded to 2 decimal places.</returns>
    public static decimal Round2(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Calculates (1 + rate)^exponent in a deterministic manner for compound growth calculations.
    /// Uses double internally for the power calculation, then converts back to decimal
    /// with fixed precision for consistency.
    /// </summary>
    /// <param name="rate">The rate (e.g., 0.03 for 3%).</param>
    /// <param name="exponent">The power to raise to (typically years).</param>
    /// <returns>The compound growth factor as a decimal.</returns>
    /// <remarks>
    /// This method is designed for inflation and interest calculations where
    /// the result represents a multiplication factor applied to monetary values.
    /// The result is rounded to 12 decimal places to ensure deterministic behavior
    /// across different platforms while maintaining sufficient precision.
    /// </remarks>
    public static decimal Pow1p(decimal rate, int exponent)
    {
        if (exponent == 0)
            return 1m;

        if (exponent == 1)
            return 1m + rate;

        // Use double for the power calculation
        double baseValue = (double)(1m + rate);
        double result = Math.Pow(baseValue, exponent);

        // Convert back to decimal with fixed precision for determinism
        // 12 decimal places provides sufficient precision while avoiding
        // floating-point representation issues
        return Math.Round((decimal)result, 12, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Calculates the monthly interest rate from an annual rate using compound interest.
    /// Uses the formula: monthlyRate = (1 + annualRate)^(1/12) - 1
    /// </summary>
    /// <param name="annualRate">The annual interest rate (e.g., 0.03 for 3%).</param>
    /// <returns>The equivalent monthly rate as a decimal with 16 decimal places precision.</returns>
    /// <remarks>
    /// The result is stored with high precision (16 decimal places) to minimize
    /// compounding errors when applied across 12 months. This matches Excel's
    /// internal precision for similar calculations.
    /// </remarks>
    public static decimal MonthlyRateFromAnnual(decimal annualRate)
    {
        if (annualRate == 0m)
            return 0m;

        // Calculate monthly rate: (1 + annual)^(1/12) - 1
        double annual = (double)(1m + annualRate);
        double monthlyFactor = Math.Pow(annual, 1.0 / 12.0);
        double monthlyRate = monthlyFactor - 1.0;

        // Store with high precision for accuracy across 12 monthly calculations
        return Math.Round((decimal)monthlyRate, 16, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Validates that a rate is within a reasonable range.
    /// </summary>
    /// <param name="rate">The rate to validate.</param>
    /// <param name="minRate">Minimum allowed rate (inclusive).</param>
    /// <param name="maxRate">Maximum allowed rate (inclusive).</param>
    /// <returns>True if the rate is valid; otherwise false.</returns>
    public static bool IsValidRate(decimal rate, decimal minRate = -0.5m, decimal maxRate = 1.0m)
    {
        return rate >= minRate && rate <= maxRate;
    }

    /// <summary>
    /// Calculates the future value of a present amount after compound growth.
    /// </summary>
    /// <param name="presentValue">The current value.</param>
    /// <param name="rate">The growth rate per period.</param>
    /// <param name="periods">The number of periods.</param>
    /// <returns>The future value, rounded to 2 decimal places.</returns>
    public static decimal FutureValue(decimal presentValue, decimal rate, int periods)
    {
        return Round2(presentValue * Pow1p(rate, periods));
    }
}
