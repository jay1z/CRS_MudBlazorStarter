using CRS.Core.ReserveCalculator.Models;

namespace CRS.Core.ReserveCalculator.Services;

/// <summary>
/// Main orchestrator for reserve study calculations.
/// Coordinates expenditure scheduling, contribution strategy, and funding plan generation.
/// 
/// This class is pure and has no dependencies on EF, UI, or tenancy.
/// All input comes through ReserveStudyInput, all output through ReserveStudyResult.
/// </summary>
public class ReserveStudyCalculator
{
    private readonly ExpenditureScheduleService _expenditureService;
    private readonly ContributionStrategyService _contributionService;
    private readonly FundingPlanService _fundingPlanService;

    public ReserveStudyCalculator()
    {
        _expenditureService = new ExpenditureScheduleService();
        _contributionService = new ContributionStrategyService();
        _fundingPlanService = new FundingPlanService();
    }

    /// <summary>
    /// Calculates the complete reserve study projection.
    /// </summary>
    /// <param name="input">The validated reserve study input.</param>
    /// <returns>The calculation result with yearly projections, allocations, and graph data.</returns>
    public ReserveStudyResult Calculate(ReserveStudyInput input)
    {
        // Validate inputs
        var validationErrors = input.Validate();
        var errors = validationErrors.Where(e => !e.StartsWith("Warning:")).ToList();
        var warnings = validationErrors.Where(e => e.StartsWith("Warning:")).ToList();

        if (errors.Count > 0)
        {
            return ReserveStudyResult.Failure(string.Join("; ", errors));
        }

        try
        {
            // Step 1: Generate expenditure schedule
            var expenditureSchedule = _expenditureService.Generate(input);

            // Step 2: Build contribution schedule based on strategy
            var contributions = _contributionService.BuildContributions(
                input,
                expenditureSchedule.TotalExpendituresByYear);

            // Step 3: Build year-by-year funding plan with interest
            var years = _fundingPlanService.BuildPlan(
                input,
                contributions,
                expenditureSchedule.TotalExpendituresByYear);

            // Step 4: Calculate category allocations
            var allocations = CalculateCategoryAllocations(input, expenditureSchedule);

            // Step 5: Build graph series
            var graph = BuildGraphSeries(input, years);

            // Assemble result
            return new ReserveStudyResult
            {
                StartYear = input.StartYear,
                ProjectionYears = input.ProjectionYears,
                Years = years,
                Allocation = allocations,
                Graph = graph,
                ExpenditureSchedule = expenditureSchedule,
                Warnings = warnings,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return ReserveStudyResult.Failure($"Calculation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates category allocations from component expenditure schedules.
    /// </summary>
    private IReadOnlyList<CategoryAllocation> CalculateCategoryAllocations(
        ReserveStudyInput input,
        ExpenditureSchedule schedule)
    {
        // Group components by category and sum their expenditures
        var categoryTotals = new Dictionary<string, (decimal total, int count)>();

        foreach (var component in input.Components)
        {
            var key = component.Id ?? component.Name;
            if (!schedule.ComponentSchedules.TryGetValue(key, out var componentSchedule))
                continue;

            var componentTotal = componentSchedule.Sum();
            var category = component.Category ?? "General";

            if (categoryTotals.TryGetValue(category, out var existing))
            {
                categoryTotals[category] = (existing.total + componentTotal, existing.count + 1);
            }
            else
            {
                categoryTotals[category] = (componentTotal, 1);
            }
        }

        var grandTotal = schedule.GrandTotal;
        var allocations = new List<CategoryAllocation>();

        foreach (var kvp in categoryTotals.OrderByDescending(x => x.Value.total))
        {
            var percentOfTotal = grandTotal > 0
                ? Money.Round2((kvp.Value.total / grandTotal) * 100m)
                : 0m;

            allocations.Add(new CategoryAllocation
            {
                Category = kvp.Key,
                TotalSpend = Money.Round2(kvp.Value.total),
                PercentOfTotal = percentOfTotal,
                ComponentCount = kvp.Value.count
            });
        }

        return allocations;
    }

    /// <summary>
    /// Builds graph series arrays from year results.
    /// </summary>
    private GraphSeries BuildGraphSeries(ReserveStudyInput input, IReadOnlyList<YearResult> years)
    {
        return new GraphSeries
        {
            Years = years.Select(y => y.CalendarYear).ToArray(),
            Expenditures = years.Select(y => y.Expenditures).ToArray(),
            Contributions = years.Select(y => y.Contribution).ToArray(),
            EndingBalances = years.Select(y => y.EndingBalance).ToArray(),
            InterestEarned = years.Select(y => y.InterestEarned).ToArray()
        };
    }
}
