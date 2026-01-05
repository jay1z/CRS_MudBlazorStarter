using CRS.Core.ReserveCalculator.Enums;
using CRS.Core.ReserveCalculator.Models;

namespace CRS.Core.ReserveCalculator.Services;

/// <summary>
/// Builds the year-by-year funding plan with interest calculations.
/// Supports both annual average balance and monthly simulation interest models.
/// </summary>
public class FundingPlanService
{
    /// <summary>
    /// Builds the complete funding plan with all year results.
    /// </summary>
    /// <param name="input">The reserve study input.</param>
    /// <param name="contributionsAnnual">Annual contribution amounts (0-based array).</param>
    /// <param name="expendituresAnnual">Annual expenditure amounts (0-based array).</param>
    /// <returns>Year-by-year results for the projection period.</returns>
    public IReadOnlyList<YearResult> BuildPlan(
        ReserveStudyInput input,
        decimal[] contributionsAnnual,
        decimal[] expendituresAnnual)
    {
        return input.InterestModel switch
        {
            InterestModel.AnnualAverageBalance =>
                BuildPlanWithAnnualInterest(input, contributionsAnnual, expendituresAnnual),
            InterestModel.MonthlySimulation =>
                BuildPlanWithMonthlySimulation(input, contributionsAnnual, expendituresAnnual),
            _ => throw new ArgumentException($"Unknown interest model: {input.InterestModel}")
        };
    }

    /// <summary>
    /// Builds the funding plan using annual average balance for interest calculation.
    /// 
    /// Average balance calculation depends on contribution and expenditure timing:
    /// - StartOfPeriod contributions are available all year
    /// - EndOfPeriod contributions are available at year end only
    /// - MidPeriod contributions are available for half the year
    /// 
    /// Interest = AverageBalance * AnnualInterestRate
    /// </summary>
    private IReadOnlyList<YearResult> BuildPlanWithAnnualInterest(
        ReserveStudyInput input,
        decimal[] contributionsAnnual,
        decimal[] expendituresAnnual)
    {
        var results = new List<YearResult>();
        decimal balance = input.StartingBalance;

        for (int i = 0; i < input.ProjectionYears; i++)
        {
            int yearIndex = i + 1;
            int calendarYear = input.StartYear + i;
            decimal contribution = contributionsAnnual[i];
            decimal expenditure = expendituresAnnual[i];

            decimal beginningBalance = balance;

            // Calculate average balance based on timing
            decimal averageBalance = CalculateAverageBalance(
                beginningBalance,
                contribution,
                expenditure,
                input.ContributionTiming,
                input.ExpenditureTiming);

            // Interest on average balance (no interest on negative balances)
            decimal interest = averageBalance > 0
                ? Money.Round2(averageBalance * input.InterestRateAnnual)
                : 0m;

            // Calculate ending balance
            decimal endingBalance = Money.Round2(
                beginningBalance + contribution + interest - expenditure);

            results.Add(new YearResult
            {
                YearIndex = yearIndex,
                CalendarYear = calendarYear,
                BeginningBalance = Money.Round2(beginningBalance),
                Contribution = contribution,
                InterestEarned = interest,
                Expenditures = expenditure,
                EndingBalance = endingBalance
            });

            balance = endingBalance;
        }

        return results;
    }

    /// <summary>
    /// Calculates average balance for interest based on timing assumptions.
    /// 
    /// Simplified model:
    /// - StartOfPeriod contribution: fully available for interest
    /// - MidPeriod contribution: 50% available for interest
    /// - EndOfPeriod contribution: not available for interest
    /// 
    /// Similarly for expenditures (which reduce the balance):
    /// - StartOfYear: reduces beginning balance immediately
    /// - MidYear: reduces balance halfway through
    /// - EndOfYear: balance available until year end
    /// - MonthlySpread: average of 50% reduction throughout year
    /// </summary>
    private decimal CalculateAverageBalance(
        decimal beginningBalance,
        decimal contribution,
        decimal expenditure,
        Timing contributionTiming,
        ExpenditureTiming expenditureTiming)
    {
        // Contribution weight (how much of the year the contribution is available)
        decimal contributionWeight = contributionTiming switch
        {
            Timing.StartOfPeriod => 1.0m,
            Timing.MidPeriod => 0.5m,
            Timing.EndOfPeriod => 0.0m,
            _ => 0.5m
        };

        // Expenditure weight (how much of the year before expenditure reduces balance)
        decimal expenditureWeight = expenditureTiming switch
        {
            ExpenditureTiming.StartOfYear => 0.0m, // Reduces immediately
            ExpenditureTiming.MidYear => 0.5m,
            ExpenditureTiming.EndOfYear => 1.0m,
            ExpenditureTiming.MonthlySpread => 0.5m,
            _ => 0.5m
        };

        // Average balance = beginning + (contribution * weight) - (expenditure * (1 - weight))
        decimal avgFromContribution = contribution * contributionWeight;
        decimal avgExpenditureImpact = expenditure * (1.0m - expenditureWeight);

        return beginningBalance + avgFromContribution - avgExpenditureImpact;
    }

    /// <summary>
    /// Builds the funding plan using monthly simulation for interest calculation.
    /// This provides Excel-like accuracy by simulating 12 months per year.
    /// 
    /// Monthly rate = (1 + annualRate)^(1/12) - 1
    /// 
    /// Each month:
    /// 1. Apply deposits (based on frequency and timing)
    /// 2. Apply expenditures (based on timing)
    /// 3. Calculate and apply interest: balance * monthlyRate
    /// </summary>
    private IReadOnlyList<YearResult> BuildPlanWithMonthlySimulation(
        ReserveStudyInput input,
        decimal[] contributionsAnnual,
        decimal[] expendituresAnnual)
    {
        var results = new List<YearResult>();
        decimal balance = input.StartingBalance;
        decimal monthlyRate = Money.MonthlyRateFromAnnual(input.InterestRateAnnual);

        for (int i = 0; i < input.ProjectionYears; i++)
        {
            int yearIndex = i + 1;
            int calendarYear = input.StartYear + i;
            decimal annualContribution = contributionsAnnual[i];
            decimal annualExpenditure = expendituresAnnual[i];

            decimal beginningBalance = balance;
            decimal yearInterest = 0m;

            // Calculate monthly deposits
            var monthlyDeposits = CalculateMonthlyDeposits(
                annualContribution,
                input.ContributionFrequency,
                input.ContributionTiming);

            // Calculate monthly expenditures
            var monthlyExpenditures = CalculateMonthlyExpenditures(
                annualExpenditure,
                input.ExpenditureTiming);

            // Simulate 12 months
            for (int month = 1; month <= 12; month++)
            {
                // Apply deposit
                balance += monthlyDeposits[month - 1];

                // Apply expenditure
                balance -= monthlyExpenditures[month - 1];

                // Calculate interest (no interest on negative balance)
                if (balance > 0)
                {
                    decimal monthInterest = balance * monthlyRate;
                    yearInterest += monthInterest;
                    balance += monthInterest;
                }
            }

            // Round final values
            yearInterest = Money.Round2(yearInterest);
            decimal endingBalance = Money.Round2(balance);

            results.Add(new YearResult
            {
                YearIndex = yearIndex,
                CalendarYear = calendarYear,
                BeginningBalance = Money.Round2(beginningBalance),
                Contribution = annualContribution,
                InterestEarned = yearInterest,
                Expenditures = annualExpenditure,
                EndingBalance = endingBalance
            });
        }

        return results;
    }

    /// <summary>
    /// Calculates how deposits are distributed across months.
    /// </summary>
    private decimal[] CalculateMonthlyDeposits(
        decimal annualContribution,
        ContributionFrequency frequency,
        Timing timing)
    {
        var monthly = new decimal[12];

        if (frequency == ContributionFrequency.Monthly)
        {
            // Monthly contributions: divide by 12
            decimal monthlyAmount = annualContribution / 12m;
            for (int i = 0; i < 12; i++)
            {
                monthly[i] = monthlyAmount;
            }
        }
        else // Annual
        {
            // Single deposit based on timing
            int depositMonth = timing switch
            {
                Timing.StartOfPeriod => 1,   // January
                Timing.MidPeriod => 6,       // June
                Timing.EndOfPeriod => 12,    // December
                _ => 1
            };
            monthly[depositMonth - 1] = annualContribution;
        }

        return monthly;
    }

    /// <summary>
    /// Calculates how expenditures are distributed across months.
    /// </summary>
    private decimal[] CalculateMonthlyExpenditures(
        decimal annualExpenditure,
        ExpenditureTiming timing)
    {
        var monthly = new decimal[12];

        switch (timing)
        {
            case ExpenditureTiming.StartOfYear:
                monthly[0] = annualExpenditure; // January
                break;

            case ExpenditureTiming.MidYear:
                monthly[5] = annualExpenditure; // June
                break;

            case ExpenditureTiming.EndOfYear:
                monthly[11] = annualExpenditure; // December
                break;

            case ExpenditureTiming.MonthlySpread:
                decimal monthlyAmount = annualExpenditure / 12m;
                for (int i = 0; i < 12; i++)
                {
                    monthly[i] = monthlyAmount;
                }
                break;
        }

        return monthly;
    }
}
