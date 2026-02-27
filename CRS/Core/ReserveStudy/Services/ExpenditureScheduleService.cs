using Horizon.Core.ReserveCalculator.Enums;
using Horizon.Core.ReserveCalculator.Models;

namespace Horizon.Core.ReserveCalculator.Services;

/// <summary>
/// Generates expenditure schedules for reserve study components.
/// This service calculates when and how much will be spent on each component
/// over the projection period, accounting for inflation.
/// </summary>
public class ExpenditureScheduleService
{
    /// <summary>
    /// Generates the complete expenditure schedule for all components.
    /// </summary>
    /// <param name="input">The reserve study input.</param>
    /// <returns>An expenditure schedule with per-component and total expenditures.</returns>
    public ExpenditureSchedule Generate(ReserveStudyInput input)
    {
        var result = new ExpenditureSchedule
        {
            ComponentSchedules = new Dictionary<string, decimal[]>(),
            TotalExpendituresByYear = new decimal[input.ProjectionYears]
        };

        foreach (var component in input.Components)
        {
            var componentKey = component.Id ?? component.Name;
            var schedule = GenerateComponentSchedule(input, component);

            // Apply rounding policy
            if (input.RoundingPolicy == RoundingPolicy.PerComponentPerYear)
            {
                for (int i = 0; i < schedule.Length; i++)
                {
                    schedule[i] = Money.Round2(schedule[i]);
                }
            }

            result.ComponentSchedules[componentKey] = schedule;

            // Add to totals
            for (int i = 0; i < schedule.Length; i++)
            {
                result.TotalExpendituresByYear[i] += schedule[i];
            }
        }

        // Apply rounding to totals if using TotalOnly policy
        if (input.RoundingPolicy == RoundingPolicy.PerYearTotalsOnly)
        {
            for (int i = 0; i < result.TotalExpendituresByYear.Length; i++)
            {
                result.TotalExpendituresByYear[i] = Money.Round2(result.TotalExpendituresByYear[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Generates the expenditure schedule for a single component.
    /// </summary>
    private decimal[] GenerateComponentSchedule(ReserveStudyInput input, ReserveComponentInput component)
    {
        var schedule = new decimal[input.ProjectionYears];
        var inflationRate = component.GetEffectiveInflationRate(input.InflationRateDefault);

        switch (component.Method)
        {
            case ComponentMethod.Replacement:
                GenerateReplacementSchedule(input, component, inflationRate, schedule);
                break;

            case ComponentMethod.PRN:
                GeneratePrnSchedule(input, component, inflationRate, schedule);
                break;

            case ComponentMethod.Combo:
                // Combo combines both Replacement and PRN schedules
                GenerateReplacementSchedule(input, component, inflationRate, schedule);
                var prnSchedule = new decimal[input.ProjectionYears];
                GeneratePrnSchedule(input, component, inflationRate, prnSchedule);
                for (int i = 0; i < schedule.Length; i++)
                {
                    schedule[i] += prnSchedule[i];
                }
                break;
        }

        return schedule;
    }

    /// <summary>
    /// Generates the expenditure schedule for a Replacement method component.
    /// 
    /// Rules:
    /// - remainingLife = RemainingLifeOverrideYears ?? ((LastServiceYear + UsefulLifeYears) - StartYear)
    /// - If remainingLife <= 0: first occurrence is yearIndex = 1
    /// - Otherwise: first occurrence is yearIndex = remainingLife + 1
    /// - Repeat every UsefulLifeYears within projection horizon
    /// - Cost in that year = CurrentCost * inflationFactor(yearIndex)
    /// 
    /// Inflation factor for yearIndex t (1..N): (1 + inflationRate)^(t-1)
    /// </summary>
    private void GenerateReplacementSchedule(
        ReserveStudyInput input,
        ReserveComponentInput component,
        decimal inflationRate,
        decimal[] schedule)
    {
        if (!component.UsefulLifeYears.HasValue || component.UsefulLifeYears.Value <= 0)
            return;

        var usefulLife = component.UsefulLifeYears.Value;
        var remainingLife = component.CalculateRemainingLife(input.StartYear);

        if (!remainingLife.HasValue)
            return;

        // Calculate first replacement year index (1-based)
        int firstYearIndex;
        if (remainingLife.Value <= 0)
        {
            // Already past due - replace in year 1
            firstYearIndex = 1;
        }
        else
        {
            // Replace after remaining life expires
            firstYearIndex = remainingLife.Value + 1;
        }

        // Generate replacements at regular intervals
        for (int yearIndex = firstYearIndex; yearIndex <= input.ProjectionYears; yearIndex += usefulLife)
        {
            // Inflation factor: (1 + rate)^(yearIndex - 1)
            // yearIndex 1 = no inflation (factor = 1)
            var inflationFactor = Money.Pow1p(inflationRate, yearIndex - 1);
            var cost = component.CurrentCost * inflationFactor;

            schedule[yearIndex - 1] += cost;
        }
    }

    /// <summary>
    /// Generates the expenditure schedule for a PRN (Periodic Repair/Maintenance) method component.
    /// 
    /// Rules:
    /// - cycleYears = CycleYears ?? 1 (default to annual)
    /// - baseCost = AnnualCostOverride ?? CurrentCost
    /// - If cycleYears == 1: every year
    /// - Otherwise: yearIndex = 1, 1+cycleYears, 1+2*cycleYears, ...
    /// - Cost = baseCost * inflationFactor(yearIndex)
    /// </summary>
    private void GeneratePrnSchedule(
        ReserveStudyInput input,
        ReserveComponentInput component,
        decimal inflationRate,
        decimal[] schedule)
    {
        var cycleYears = component.CycleYears ?? 1;
        if (cycleYears <= 0) cycleYears = 1;

        var baseCost = component.AnnualCostOverride ?? component.CurrentCost;

        // Generate PRN costs at regular intervals starting from year 1
        for (int yearIndex = 1; yearIndex <= input.ProjectionYears; yearIndex += cycleYears)
        {
            // Inflation factor: (1 + rate)^(yearIndex - 1)
            var inflationFactor = Money.Pow1p(inflationRate, yearIndex - 1);
            var cost = baseCost * inflationFactor;

            schedule[yearIndex - 1] += cost;
        }
    }
}
