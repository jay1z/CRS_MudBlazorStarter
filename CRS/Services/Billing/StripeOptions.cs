using Microsoft.Extensions.Options;
using Stripe;

namespace CRS.Services.Billing {
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

        public string? GetPriceId(CRS.Models.SubscriptionTier tier, BillingInterval interval) => (tier, interval) switch {
            (CRS.Models.SubscriptionTier.Startup, BillingInterval.Monthly) => StarterMonthlyPriceId ?? StartupPriceId,
            (CRS.Models.SubscriptionTier.Startup, BillingInterval.Yearly) => StarterYearlyPriceId,
            (CRS.Models.SubscriptionTier.Pro, BillingInterval.Monthly) => ProMonthlyPriceId ?? ProPriceId,
            (CRS.Models.SubscriptionTier.Pro, BillingInterval.Yearly) => ProYearlyPriceId,
            (CRS.Models.SubscriptionTier.Enterprise, BillingInterval.Monthly) => EnterpriseMonthlyPriceId ?? EnterprisePriceId,
            (CRS.Models.SubscriptionTier.Enterprise, BillingInterval.Yearly) => EnterpriseYearlyPriceId,
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
