using Horizon.Core.ReserveCalculator.Enums;
using Horizon.Core.ReserveCalculator.Models;

namespace Horizon.Core.ReserveCalculator.Services;

/// <summary>
/// Calculates the fully funded balance (ideal reserve) for reserve components.
/// The fully funded balance represents the accumulated depreciation-based reserve
/// that should be held for each component based on its age and replacement cost.
/// </summary>
public class FullyFundedBalanceService
{
    /// <summary>
    /// Calculates the fully funded balance for all components.
    /// </summary>
    /// <param name="input">The reserve study input.</param>
    /// <returns>The total fully funded balance across all components.</returns>
    public decimal CalculateTotalFullyFundedBalance(ReserveStudyInput input)
    {
        return input.Components.Sum(c => CalculateComponentFullyFundedBalance(c, input.StartYear));
    }

    /// <summary>
    /// Calculates the fully funded balance for a single component.
    /// 
    /// Formula: CurrentCost × (Age / UsefulLife)
    /// 
    /// Where Age = UsefulLife - RemainingLife (capped at UsefulLife)
    /// 
    /// For PRN-only components, returns 0 (no depreciation reserve needed).
    /// </summary>
    /// <param name="component">The component input.</param>
    /// <param name="startYear">The study start year.</param>
    /// <returns>The fully funded balance for this component.</returns>
    public decimal CalculateComponentFullyFundedBalance(ReserveComponentInput component, int startYear)
    {
        // PRN-only components don't have depreciation-based reserves
        if (component.Method == ComponentMethod.PRN)
            return 0m;

        var usefulLife = component.UsefulLifeYears;
        if (!usefulLife.HasValue || usefulLife.Value <= 0)
            return 0m;

        var remainingLife = component.CalculateRemainingLife(startYear);
        if (!remainingLife.HasValue)
            return 0m;

        // Age = UsefulLife - RemainingLife
        // If remaining life is negative (past due), age equals useful life
        var age = usefulLife.Value - remainingLife.Value;
        
        // Cap age at useful life (can't be more than 100% depreciated)
        age = Math.Min(age, usefulLife.Value);
        
        // Can't have negative age
        age = Math.Max(age, 0);

        // Fully funded balance = CurrentCost × (Age / UsefulLife)
        var fraction = (decimal)age / usefulLife.Value;
        return Money.Round2(component.CurrentCost * fraction);
    }

    /// <summary>
    /// Builds component summaries with fully funded balance and other report data.
    /// </summary>
    /// <param name="input">The reserve study input.</param>
    /// <param name="schedule">The expenditure schedule.</param>
    /// <returns>A list of component summaries.</returns>
    public IReadOnlyList<ComponentSummary> BuildComponentSummaries(
        ReserveStudyInput input,
        ExpenditureSchedule schedule)
    {
        var summaries = new List<ComponentSummary>();
        var totalFullyFunded = CalculateTotalFullyFundedBalance(input);

        foreach (var component in input.Components)
        {
            var key = component.Id ?? component.Name;
            var componentSchedule = schedule.ComponentSchedules.TryGetValue(key, out var sched) 
                ? sched 
                : Array.Empty<decimal>();

            var remainingLife = component.CalculateRemainingLife(input.StartYear);
            var usefulLife = component.UsefulLifeYears;
            
            // Calculate age
            int? age = null;
            if (usefulLife.HasValue && remainingLife.HasValue)
            {
                age = usefulLife.Value - remainingLife.Value;
            }

            // Find next expenditure
            int? nextExpYear = null;
            decimal? nextExpCost = null;
            for (int i = 0; i < componentSchedule.Length; i++)
            {
                if (componentSchedule[i] > 0)
                {
                    nextExpYear = input.StartYear + i;
                    nextExpCost = componentSchedule[i];
                    break;
                }
            }

            // Count expenditures
            var expenditureCount = componentSchedule.Count(x => x > 0);
            var totalExpenditure = componentSchedule.Sum();

            // Calculate fully funded balance for this component
            var componentFullyFunded = CalculateComponentFullyFundedBalance(component, input.StartYear);

            // Calculate proportional share of starting balance
            decimal currentReserve = 0m;
            if (totalFullyFunded > 0)
            {
                var proportion = componentFullyFunded / totalFullyFunded;
                currentReserve = Money.Round2(input.StartingBalance * proportion);
            }

            summaries.Add(new ComponentSummary
            {
                ComponentKey = key,
                Name = component.Name,
                Category = component.Category,
                CurrentCost = component.CurrentCost,
                UsefulLifeYears = usefulLife,
                RemainingLifeYears = remainingLife,
                AgeYears = age,
                NextExpenditureYear = nextExpYear,
                NextExpenditureCost = nextExpCost,
                TotalProjectedExpenditures = totalExpenditure,
                ExpenditureCount = expenditureCount,
                FullyFundedBalance = componentFullyFunded,
                CurrentReserve = currentReserve
            });
        }

        return summaries;
    }

    /// <summary>
    /// Calculates the special assessment amount needed to avoid any deficit.
    /// </summary>
    /// <param name="years">The year results from the funding plan.</param>
    /// <returns>The minimum additional amount needed to prevent deficits.</returns>
    public decimal CalculateSpecialAssessmentRequired(IReadOnlyList<YearResult> years)
    {
        if (years.Count == 0)
            return 0m;

        // Find the most negative balance
        var minBalance = years.Min(y => y.EndingBalance);
        
        if (minBalance >= 0)
            return 0m;

        // The special assessment needs to cover the deficit plus a small buffer
        // We add the absolute value of the minimum balance
        return Money.Round2(Math.Abs(minBalance));
    }
}
