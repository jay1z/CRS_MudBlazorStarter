using Horizon.Core.ReserveCalculator.Enums;
using Horizon.Core.ReserveCalculator.Models;

namespace Horizon.Core.ReserveCalculator.Services;

/// <summary>
/// Calculates annual contribution amounts based on the selected strategy.
/// </summary>
public class ContributionStrategyService
{
    /// <summary>
    /// Builds the annual contribution schedule based on the input strategy.
    /// </summary>
    /// <param name="input">The reserve study input.</param>
    /// <param name="expenditures">The projected annual expenditures.</param>
    /// <returns>An array of annual contribution amounts (0-based, index 0 = year 1).</returns>
    public decimal[] BuildContributions(ReserveStudyInput input, decimal[] expenditures)
    {
        var contributions = new decimal[input.ProjectionYears];

        switch (input.ContributionStrategy)
        {
            case ContributionStrategy.FixedAnnual:
                BuildFixedAnnualContributions(input, contributions);
                break;

            case ContributionStrategy.EscalatingPercent:
                BuildEscalatingContributions(input, contributions);
                break;

            case ContributionStrategy.MaintainNonNegativeBalance:
                BuildNonNegativeBalanceContributions(input, expenditures, contributions);
                break;
        }

        // Round all contributions to 2 decimal places
        for (int i = 0; i < contributions.Length; i++)
        {
            contributions[i] = Money.Round2(contributions[i]);
        }

        return contributions;
    }

    /// <summary>
    /// Fixed annual strategy: Same contribution each year.
    /// If ContributionFrequency is Monthly, the InitialAnnualContribution
    /// represents the total annual amount (sum of 12 monthly payments).
    /// </summary>
    private void BuildFixedAnnualContributions(ReserveStudyInput input, decimal[] contributions)
    {
        var annualAmount = input.InitialAnnualContribution;

        for (int i = 0; i < contributions.Length; i++)
        {
            contributions[i] = annualAmount;
        }
    }

    /// <summary>
    /// Escalating percent strategy: Contributions grow by a fixed percentage each year.
    /// Year N contribution = InitialAnnualContribution * (1 + ContributionEscalationRate)^(N-1)
    /// </summary>
    private void BuildEscalatingContributions(ReserveStudyInput input, decimal[] contributions)
    {
        for (int i = 0; i < contributions.Length; i++)
        {
            int yearIndex = i + 1; // 1-based year index
            var escalationFactor = Money.Pow1p(input.ContributionEscalationRate, yearIndex - 1);
            contributions[i] = input.InitialAnnualContribution * escalationFactor;
        }
    }

    /// <summary>
    /// Maintain non-negative balance strategy: Adjusts contributions to prevent deficit.
    /// 
    /// Algorithm:
    /// 1. Start with escalating contributions as baseline
    /// 2. For each year, calculate projected ending balance
    /// 3. If ending balance would be negative, increase that year's contribution
    /// 4. Changes propagate forward through the projection
    /// 
    /// Note: Uses a simplified interest estimate (AnnualAverageBalance model) for this calculation.
    /// The actual funding plan may differ slightly due to different interest timing.
    /// </summary>
    private void BuildNonNegativeBalanceContributions(
        ReserveStudyInput input,
        decimal[] expenditures,
        decimal[] contributions)
    {
        // Start with escalating contributions as baseline
        BuildEscalatingContributions(input, contributions);

        // Simulate year-by-year to identify and fix shortfalls
        decimal balance = input.StartingBalance;

        for (int i = 0; i < contributions.Length; i++)
        {
            // Estimate interest (simplified: on beginning balance)
            decimal estimatedInterest = Money.Round2(balance * input.InterestRateAnnual);
            if (balance < 0) estimatedInterest = 0; // No interest on negative balance

            // Calculate projected ending balance
            decimal projectedEnding = balance + contributions[i] + estimatedInterest - expenditures[i];

            // If deficit, increase contribution to cover shortfall
            if (projectedEnding < 0)
            {
                decimal shortfall = -projectedEnding;
                contributions[i] += shortfall;

                // Recalculate ending balance with adjusted contribution
                projectedEnding = balance + contributions[i] + estimatedInterest - expenditures[i];
            }

            // Update balance for next year
            balance = projectedEnding;
        }
    }
}
