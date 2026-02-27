using Microsoft.Extensions.Options;

using Stripe;

namespace Horizon.Services.Billing {
    // Strongly typed options bound from configuration (Stripe: section)
    public class StripeOptions {
        public string? SecretKey { get; set; }
        public string? PublishableKey { get; set; }
        public string? WebhookSecret { get; set; }
        // Single-price ids (kept for back-compat if you set monthly only)
        public string? StartupPriceId { get; set; }
        public string? ProPriceId { get; set; }
        public string? EnterprisePriceId { get; set; }
        // New: monthly/yearly variants
        public string? StarterMonthlyPriceId { get; set; }
        public string? StarterYearlyPriceId { get; set; }
        public string? ProMonthlyPriceId { get; set; }
        public string? ProYearlyPriceId { get; set; }
        public string? EnterpriseMonthlyPriceId { get; set; }
        public string? EnterpriseYearlyPriceId { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // TRIAL & CHECKOUT SETTINGS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Number of free trial days for new subscriptions. Set to 0 to disable trials.
        /// </summary>
        public int TrialPeriodDays { get; set; } = 14;

        /// <summary>
        /// When true, customers can enter promo/coupon codes during checkout.
        /// </summary>
        public bool AllowPromotionCodes { get; set; } = true;

        /// <summary>
        /// When true, Stripe Tax will automatically calculate and collect taxes.
        /// Requires Stripe Tax to be enabled in your Stripe dashboard.
        /// </summary>
        public bool EnableAutomaticTax { get; set; } = false;

        public string? GetPriceId(Horizon.Models.SubscriptionTier tier, BillingInterval interval) => (tier, interval) switch {
            (Horizon.Models.SubscriptionTier.Startup, BillingInterval.Monthly) => StarterMonthlyPriceId ?? StartupPriceId,
            (Horizon.Models.SubscriptionTier.Startup, BillingInterval.Yearly) => StarterYearlyPriceId,
            (Horizon.Models.SubscriptionTier.Pro, BillingInterval.Monthly) => ProMonthlyPriceId ?? ProPriceId,
            (Horizon.Models.SubscriptionTier.Pro, BillingInterval.Yearly) => ProYearlyPriceId,
            (Horizon.Models.SubscriptionTier.Enterprise, BillingInterval.Monthly) => EnterpriseMonthlyPriceId ?? EnterprisePriceId,
            (Horizon.Models.SubscriptionTier.Enterprise, BillingInterval.Yearly) => EnterpriseYearlyPriceId,
            _ => null
        };
    }

    public class BillingUrlOptions {
        public string? SuccessUrl { get; set; } // e.g. https://tenant.alxreservecloud.com/billing/success?session_id={CHECKOUT_SESSION_ID}
        public string? CancelUrl { get; set; }  // e.g. https://tenant.alxreservecloud.com/pricing/canceled
        public string? PortalReturnUrl { get; set; } // where Stripe Billing Portal returns
    }

    public interface IStripeClientFactory {
        StripeClient CreateClient();
    }

    public class StripeClientFactory : IStripeClientFactory {
        private readonly StripeOptions _options;
        public StripeClientFactory(IOptions<StripeOptions> options) {
            _options = options.Value;
        }
        public StripeClient CreateClient() => new StripeClient(_options.SecretKey ?? string.Empty);
    }
}
