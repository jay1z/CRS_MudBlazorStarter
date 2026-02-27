using Microsoft.EntityFrameworkCore;

namespace Horizon.Models;

/// <summary>
/// Represents a single milestone in a payment schedule.
/// </summary>
public class PaymentMilestone
{
    public InvoiceMilestoneType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public bool IsInvoiced { get; set; }
    public bool IsPaid { get; set; }
    public Guid? InvoiceId { get; set; }
}

/// <summary>
/// Helper class for calculating and managing payment schedules.
/// </summary>
public static class PaymentScheduleCalculator
{
    /// <summary>
    /// Gets the payment milestones for a given schedule type and proposal amount.
    /// </summary>
    public static List<PaymentMilestone> GetMilestones(
        PaymentScheduleType scheduleType, 
        decimal proposalAmount, 
        decimal? customDepositPercentage = null)
    {
        var milestones = new List<PaymentMilestone>();

        switch (scheduleType)
        {
            case PaymentScheduleType.FullPayment:
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.FullPayment,
                    Description = "Full Payment",
                    Percentage = 100m,
                    Amount = proposalAmount
                });
                break;

            case PaymentScheduleType.FiftyFifty:
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.Deposit,
                    Description = "Deposit (50%)",
                    Percentage = 50m,
                    Amount = Math.Round(proposalAmount * 0.50m, 2)
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.FinalDelivery,
                    Description = "Final Payment (50%)",
                    Percentage = 50m,
                    Amount = proposalAmount - milestones[0].Amount // Use remainder to avoid rounding issues
                });
                break;

            case PaymentScheduleType.ThirdThirdThird:
                var thirdAmount = Math.Round(proposalAmount / 3m, 2);
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.Deposit,
                    Description = "Deposit (33%)",
                    Percentage = 33m,
                    Amount = thirdAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.SiteVisitComplete,
                    Description = "Site Visit Complete (33%)",
                    Percentage = 33m,
                    Amount = thirdAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.FinalDelivery,
                    Description = "Final Payment (34%)",
                    Percentage = 34m,
                    Amount = proposalAmount - (thirdAmount * 2) // Remainder goes to final
                });
                break;

            case PaymentScheduleType.QuartersPayment:
                var quarterAmount = Math.Round(proposalAmount / 4m, 2);
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.Deposit,
                    Description = "Deposit (25%)",
                    Percentage = 25m,
                    Amount = quarterAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.SiteVisitComplete,
                    Description = "Site Visit Complete (25%)",
                    Percentage = 25m,
                    Amount = quarterAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.DraftReportDelivery,
                    Description = "Draft Report Delivery (25%)",
                    Percentage = 25m,
                    Amount = quarterAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.FinalDelivery,
                    Description = "Final Payment (25%)",
                    Percentage = 25m,
                    Amount = proposalAmount - (quarterAmount * 3) // Remainder goes to final
                });
                break;

            case PaymentScheduleType.Custom:
                var depositPct = customDepositPercentage ?? 50m;
                var depositAmount = Math.Round(proposalAmount * (depositPct / 100m), 2);
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.Deposit,
                    Description = $"Deposit ({depositPct}%)",
                    Percentage = depositPct,
                    Amount = depositAmount
                });
                milestones.Add(new PaymentMilestone
                {
                    Type = InvoiceMilestoneType.FinalDelivery,
                    Description = $"Final Payment ({100m - depositPct}%)",
                    Percentage = 100m - depositPct,
                    Amount = proposalAmount - depositAmount
                });
                break;
        }

        return milestones;
    }

    /// <summary>
    /// Gets a user-friendly display name for the payment schedule type.
    /// </summary>
    public static string GetScheduleDisplayName(PaymentScheduleType type) => type switch
    {
        PaymentScheduleType.FullPayment => "Full Payment",
        PaymentScheduleType.FiftyFifty => "50% Deposit, 50% on Completion",
        PaymentScheduleType.ThirdThirdThird => "3 Payments (33% each)",
        PaymentScheduleType.QuartersPayment => "4 Payments (25% each)",
        PaymentScheduleType.Custom => "Custom Schedule",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets a user-friendly display name for the milestone type.
    /// </summary>
    public static string GetMilestoneDisplayName(InvoiceMilestoneType type) => type switch
    {
        InvoiceMilestoneType.Deposit => "Deposit",
        InvoiceMilestoneType.SiteVisitComplete => "Site Visit Complete",
        InvoiceMilestoneType.DraftReportDelivery => "Draft Report Delivery",
        InvoiceMilestoneType.FinalDelivery => "Final Delivery",
        InvoiceMilestoneType.FullPayment => "Full Payment",
        InvoiceMilestoneType.Custom => "Custom",
        _ => type.ToString()
    };

        /// <summary>
        /// Calculates the prepayment discount amount.
        /// </summary>
        public static decimal CalculatePrepaymentDiscount(decimal amount, decimal discountPercentage)
        {
            return Math.Round(amount * (discountPercentage / 100m), 2);
        }

        /// <summary>
        /// Calculates the early payment discount amount.
        /// </summary>
        public static decimal CalculateEarlyPaymentDiscount(decimal amount, decimal discountPercentage)
        {
            return Math.Round(amount * (discountPercentage / 100m), 2);
        }

        /// <summary>
        /// Calculates late payment interest for a given period.
        /// </summary>
        /// <param name="principalAmount">The unpaid balance</param>
        /// <param name="monthlyInterestRate">Monthly interest rate as percentage (e.g., 1.5 for 1.5%)</param>
        /// <param name="daysOverdue">Number of days past the interest start date</param>
        /// <returns>Interest amount</returns>
        public static decimal CalculateLateInterest(decimal principalAmount, decimal monthlyInterestRate, int daysOverdue)
        {
            if (principalAmount <= 0 || monthlyInterestRate <= 0 || daysOverdue <= 0)
                return 0m;

            // Convert monthly rate to daily rate (assuming 30 days per month)
            var dailyRate = monthlyInterestRate / 100m / 30m;
            var interest = principalAmount * dailyRate * daysOverdue;
            return Math.Round(interest, 2);
        }

        /// <summary>
        /// Gets the early payment discount date based on invoice date and discount days.
        /// </summary>
        public static DateTime GetEarlyPaymentDiscountDate(DateTime invoiceDate, int discountDays)
        {
            return invoiceDate.AddDays(discountDays);
        }

        /// <summary>
        /// Gets the late interest start date based on due date and grace period.
        /// </summary>
        public static DateTime GetLateInterestStartDate(DateTime dueDate, int gracePeriodDays)
        {
            return dueDate.AddDays(gracePeriodDays);
        }

        /// <summary>
        /// Formats the payment terms as a display string (e.g., "2/10 Net 30")
        /// </summary>
        public static string FormatPaymentTerms(
            int paymentDueDays,
            decimal earlyDiscountPercentage = 0,
            int earlyDiscountDays = 0,
            decimal lateInterestRate = 0)
        {
            var parts = new List<string>();

            // Early payment discount (2/10 format)
            if (earlyDiscountPercentage > 0 && earlyDiscountDays > 0)
            {
                parts.Add($"{earlyDiscountPercentage:0.#}/{earlyDiscountDays}");
            }

            // Net days
            parts.Add($"Net {paymentDueDays}");

            // Late interest
            if (lateInterestRate > 0)
            {
                parts.Add($"{lateInterestRate:0.#}% monthly interest if overdue");
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets the effective deposit amount considering minimum deposit requirement.
        /// </summary>
        public static decimal GetEffectiveDepositAmount(
            decimal proposalAmount,
            decimal depositPercentage,
            decimal? minimumDepositAmount)
        {
            var calculatedDeposit = Math.Round(proposalAmount * (depositPercentage / 100m), 2);

            if (minimumDepositAmount.HasValue && calculatedDeposit < minimumDepositAmount.Value)
            {
                return minimumDepositAmount.Value;
            }

            return calculatedDeposit;
        }
    }
