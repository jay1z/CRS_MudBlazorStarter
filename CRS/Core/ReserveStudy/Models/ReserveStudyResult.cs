using CRS.Core.ReserveCalculator.Enums;

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

    #region New Report/Narrative Properties

    /// <summary>
    /// Summary information for each component.
    /// Used for detailed component tables in reports.
    /// </summary>
    public IReadOnlyList<ComponentSummary> ComponentSummaries { get; set; } = Array.Empty<ComponentSummary>();

    /// <summary>
    /// The ideal (fully funded) reserve balance at the start of the study.
    /// Calculated as the sum of accumulated depreciation for all components.
    /// </summary>
    public decimal FullyFundedBalance { get; set; }

    /// <summary>
    /// The starting reserve balance provided in the input.
    /// </summary>
    public decimal StartingBalance { get; set; }

    /// <summary>
    /// Percent funded at the start of the study.
    /// Formula: (StartingBalance / FullyFundedBalance) × 100
    /// </summary>
    public decimal PercentFunded => FullyFundedBalance > 0
        ? Money.Round2((StartingBalance / FullyFundedBalance) * 100m)
        : 100m;

    /// <summary>
    /// Funding status classification based on percent funded.
    /// Strong (≥70%), Fair (30-69%), Weak (&lt;30%)
    /// </summary>
    public FundingStatus FundingStatus => PercentFunded switch
    {
        >= 70m => FundingStatus.Strong,
        >= 30m => FundingStatus.Fair,
        _ => FundingStatus.Weak
    };

    /// <summary>
    /// Monthly equivalent of the first year's annual contribution.
    /// Useful for narrative: "Your monthly contribution of $X per unit..."
    /// </summary>
    public decimal MonthlyContributionYear1 => Years.Count > 0
        ? Money.Round2(Years[0].Contribution / 12m)
        : 0m;

    /// <summary>
    /// Monthly contribution at the end of the projection period.
    /// Shows the contribution growth over time.
    /// </summary>
    public decimal MonthlyContributionFinalYear => Years.Count > 0
        ? Money.Round2(Years[^1].Contribution / 12m)
        : 0m;

    /// <summary>
    /// The annual contribution for the first year.
    /// </summary>
    public decimal AnnualContributionYear1 => Years.Count > 0
        ? Years[0].Contribution
        : 0m;

    /// <summary>
    /// The annual contribution for the final year.
    /// </summary>
    public decimal AnnualContributionFinalYear => Years.Count > 0
        ? Years[^1].Contribution
        : 0m;

    /// <summary>
    /// The amount needed as a special assessment to avoid any deficit.
    /// Zero if no deficit years exist.
    /// </summary>
    public decimal SpecialAssessmentRequired { get; set; }

    /// <summary>
    /// The threshold balance below which a special assessment warning is triggered.
    /// Default is 10% of fully funded balance.
    /// </summary>
    public decimal SpecialAssessmentThreshold => Money.Round2(FullyFundedBalance * 0.10m);

    /// <summary>
    /// Indicates if a special assessment may be required based on threshold.
    /// </summary>
    public bool SpecialAssessmentWarning => MinimumBalance < SpecialAssessmentThreshold;

    /// <summary>
    /// The year with the highest expenditure.
    /// </summary>
    public int? PeakExpenditureYear => Years.Count > 0
        ? Years.OrderByDescending(y => y.Expenditures).First().CalendarYear
        : null;

    /// <summary>
    /// The highest annual expenditure amount.
    /// </summary>
    public decimal PeakExpenditureAmount => Years.Count > 0
        ? Years.Max(y => y.Expenditures)
        : 0m;

    /// <summary>
    /// Number of components that are past their useful life.
    /// </summary>
    public int PastDueComponentCount => ComponentSummaries.Count(c => c.IsPastDue);

    /// <summary>
    /// Number of components due within the next 5 years.
    /// </summary>
    public int DueSoonComponentCount => ComponentSummaries.Count(c => c.IsDueSoon);

    /// <summary>
    /// Total current replacement cost of all components.
    /// </summary>
    public decimal TotalCurrentReplacementCost => ComponentSummaries.Sum(c => c.CurrentCost);

    #endregion

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
        ? Money.Round2(TotalContributions / Years.Count)
        : 0m;

    /// <summary>
    /// Average annual expenditure.
    /// </summary>
    public decimal AverageAnnualExpenditure => Years.Count > 0 
        ? Money.Round2(TotalExpenditures / Years.Count)
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
