namespace Horizon.Core.ReserveCalculator.Models;

/// <summary>
/// Result for a single year in the funding plan projection.
/// </summary>
public class YearResult
{
    /// <summary>
    /// Year index within the projection (1-based, 1..N).
    /// </summary>
    public int YearIndex { get; set; }

    /// <summary>
    /// The calendar year this result represents.
    /// </summary>
    public int CalendarYear { get; set; }

    /// <summary>
    /// Fund balance at the beginning of the year.
    /// </summary>
    public decimal BeginningBalance { get; set; }

    /// <summary>
    /// Total contributions made during the year.
    /// </summary>
    public decimal Contribution { get; set; }

    /// <summary>
    /// Interest earned during the year.
    /// </summary>
    public decimal InterestEarned { get; set; }

    /// <summary>
    /// Total expenditures during the year.
    /// </summary>
    public decimal Expenditures { get; set; }

    /// <summary>
    /// Fund balance at the end of the year.
    /// BeginningBalance + Contribution + InterestEarned - Expenditures
    /// </summary>
    public decimal EndingBalance { get; set; }

    /// <summary>
    /// Net cash flow for the year (Contribution - Expenditures).
    /// </summary>
    public decimal NetCashFlow => Contribution - Expenditures;

    /// <summary>
    /// Indicates if the ending balance went negative during this year.
    /// </summary>
    public bool IsDeficitYear => EndingBalance < 0;
}
