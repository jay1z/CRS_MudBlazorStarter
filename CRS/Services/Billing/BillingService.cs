using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CRS.Services.Billing {
    public interface IBillingService {
        Task<CRS.Models.Tenant> EnsureStripeCustomerAsync(int tenantId, CancellationToken ct = default);
        Task<string> CreateCheckoutSessionAsync(int tenantId, SubscriptionTier tier, BillingInterval interval, CancellationToken ct = default);
        Task<string> CreateBillingPortalSessionAsync(int tenantId, CancellationToken ct = default);
        Task ApplySubscriptionUpdateAsync(string subscriptionId, string customerId, SubscriptionTier? tier, SubscriptionStatus status, CancellationToken ct = default);
    }

    public class BillingService : IBillingService {
        private readonly ApplicationDbContext _db;
        private readonly IStripeClientFactory _clientFactory;
        private readonly StripeOptions _stripeOptions;
        private readonly BillingUrlOptions _urlOptions;
        private readonly ILogger<BillingService> _logger;

        public BillingService(ApplicationDbContext db, IStripeClientFactory clientFactory, IOptions<StripeOptions> stripeOptions, IOptions<BillingUrlOptions> urlOptions, ILogger<BillingService> logger) {
            _db = db;
            _clientFactory = clientFactory;
            _stripeOptions = stripeOptions.Value;
            _urlOptions = urlOptions.Value;
            _logger = logger;
        }

        public async Task<CRS.Models.Tenant> EnsureStripeCustomerAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);
            if (!string.IsNullOrWhiteSpace(tenant.StripeCustomerId)) return tenant;

            var client = _clientFactory.CreateClient();
            var customerService = new CustomerService(client);
            var customer = await customerService.CreateAsync(new CustomerCreateOptions {
                Name = tenant.Name,
                Metadata = new Dictionary<string, string> {
                    ["tenant_id"] = tenant.Id.ToString(),
                    ["tenant_public_id"] = tenant.PublicId.ToString()
                }
            }, null, ct);
            tenant.StripeCustomerId = customer.Id;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return tenant;
        }

        public async Task<string> CreateCheckoutSessionAsync(int tenantId, SubscriptionTier tier, BillingInterval interval, CancellationToken ct = default) {
            var tenant = await EnsureStripeCustomerAsync(tenantId, ct);
            var priceId = _stripeOptions.GetPriceId(tier, interval);
            if (string.IsNullOrWhiteSpace(priceId)) throw new InvalidOperationException($"No Stripe price id configured for {tier} {interval}");

            var client = _clientFactory.CreateClient();
            var sessionService = new SessionService(client);
            var session = await sessionService.CreateAsync(new SessionCreateOptions {
                Mode = "subscription",
                Customer = tenant.StripeCustomerId,
                SuccessUrl = _urlOptions.SuccessUrl?.Replace("{CHECKOUT_SESSION_ID}", "{CHECKOUT_SESSION_ID}") ?? "https://localhost:5001/billing/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _urlOptions.CancelUrl ?? "https://localhost:5001/pricing",
                LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
                Metadata = new Dictionary<string, string> { ["tenant_id"] = tenant.Id.ToString(), ["tier"] = tier.ToString(), ["interval"] = interval.ToString() }
            }, null, ct);
            _logger.LogInformation("Created checkout session {SessionId} for tenant {TenantId} tier {Tier} interval {Interval}", session.Id, tenant.Id, tier, interval);
            return session.Url;
        }

        public async Task<string> CreateBillingPortalSessionAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);
            if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId)) throw new InvalidOperationException("Tenant has no Stripe customer id");
            var client = _clientFactory.CreateClient();
            var portalService = new Stripe.BillingPortal.SessionService(client);
            var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions {
                Customer = tenant.StripeCustomerId,
                ReturnUrl = _urlOptions.PortalReturnUrl ?? _urlOptions.SuccessUrl ?? "https://localhost:5001/account/billing"
            }, null, ct);
            _logger.LogInformation("Created portal session {PortalSessionId} for tenant {TenantId}", portalSession.Id, tenant.Id);
            return portalSession.Url;
        }

        public async Task ApplySubscriptionUpdateAsync(string subscriptionId, string customerId, SubscriptionTier? tier, SubscriptionStatus status, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == customerId || t.StripeSubscriptionId == subscriptionId, ct);
            if (tenant == null) {
                _logger.LogWarning("Subscription update: tenant not found for subscription {SubscriptionId} customer {CustomerId}", subscriptionId, customerId);
                return;
            }
            if (!string.IsNullOrEmpty(subscriptionId)) tenant.StripeSubscriptionId = subscriptionId;
            if (tier.HasValue) {
                tenant.Tier = tier.Value;
                var limits = SubscriptionTierDefaults.GetLimits(tier.Value);
                if (tier.Value != SubscriptionTier.Enterprise) { tenant.MaxCommunities = limits.communities; tenant.MaxSpecialistUsers = limits.specialists; }
                else { tenant.MaxCommunities = Math.Max(tenant.MaxCommunities, limits.communities); tenant.MaxSpecialistUsers = Math.Max(tenant.MaxSpecialistUsers, limits.specialists); }
            }
            tenant.SubscriptionStatus = status;
            tenant.IsActive = status == SubscriptionStatus.Active || status == SubscriptionStatus.Trialing;
            if (status == SubscriptionStatus.Canceled) tenant.SubscriptionCanceledAt = DateTime.UtcNow;
            if (status == SubscriptionStatus.Active && tenant.SubscriptionActivatedAt == null) tenant.SubscriptionActivatedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Applied subscription update {SubscriptionId} status {Status} to tenant {TenantId}", subscriptionId, status, tenant.Id);
        }
    }
}
