namespace CRS.Core.ReserveCalculator.Models;

/// <summary>
/// Complete result of a reserve study calculation.
/// </summary>
public class ReserveStudyResult
{
    /// <summary>
    /// The first calendar year of the projection.
    /// </summary>
    public int StartYear { get; set; }

    /// <summary>
    /// Number of years in the projection.
    /// </summary>
    public int ProjectionYears { get; set; }

    /// <summary>
    /// Detailed results for each year.
    /// </summary>
    public IReadOnlyList<YearResult> Years { get; set; } = Array.Empty<YearResult>();

    /// <summary>
    /// Allocation of expenditures by category.
    /// </summary>
    public IReadOnlyList<CategoryAllocation> Allocation { get; set; } = Array.Empty<CategoryAllocation>();

    /// <summary>
    /// Data series for graphing.
    /// </summary>
    public GraphSeries Graph { get; set; } = new();

    /// <summary>
    /// The expenditure schedule showing per-component costs.
    /// </summary>
    public ExpenditureSchedule ExpenditureSchedule { get; set; } = new();

    /// <summary>
    /// Any validation warnings or notes about the calculation.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indicates if the calculation completed successfully.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message if calculation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    #region Computed Summary Properties

    /// <summary>
    /// Total contributions over the projection period.
    /// </summary>
    public decimal TotalContributions => Years.Sum(y => y.Contribution);

    /// <summary>
    /// Total expenditures over the projection period.
    /// </summary>
    public decimal TotalExpenditures => Years.Sum(y => y.Expenditures);

    /// <summary>
    /// Total interest earned over the projection period.
    /// </summary>
    public decimal TotalInterestEarned => Years.Sum(y => y.InterestEarned);

    /// <summary>
    /// Final ending balance at the end of the projection.
    /// </summary>
    public decimal FinalBalance => Years.Count > 0 ? Years[^1].EndingBalance : 0m;

    /// <summary>
    /// Minimum ending balance encountered during the projection.
    /// </summary>
    public decimal MinimumBalance => Years.Count > 0 ? Years.Min(y => y.EndingBalance) : 0m;

    /// <summary>
    /// Year with the minimum ending balance.
    /// </summary>
    public int? MinimumBalanceYear => Years.Count > 0 
        ? Years.OrderBy(y => y.EndingBalance).First().CalendarYear 
        : null;

    /// <summary>
    /// Number of years with a deficit (negative ending balance).
    /// </summary>
    public int DeficitYearCount => Years.Count(y => y.IsDeficitYear);

    /// <summary>
    /// First year with a deficit, if any.
    /// </summary>
    public int? FirstDeficitYear => Years.FirstOrDefault(y => y.IsDeficitYear)?.CalendarYear;

    /// <summary>
    /// Indicates if the funding plan is fully funded (no deficit years).
    /// </summary>
    public bool IsFullyFunded => DeficitYearCount == 0;

    /// <summary>
    /// Average annual contribution.
    /// </summary>
    public decimal AverageAnnualContribution => Years.Count > 0 
        ? TotalContributions / Years.Count 
        : 0m;

    /// <summary>
    /// Average annual expenditure.
    /// </summary>
    public decimal AverageAnnualExpenditure => Years.Count > 0 
        ? TotalExpenditures / Years.Count 
        : 0m;

    #endregion

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static ReserveStudyResult Failure(string errorMessage)
    {
        return new ReserveStudyResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
