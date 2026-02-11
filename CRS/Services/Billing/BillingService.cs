using System.Linq;
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
        Task<string> CreateDeferredTenantCheckoutSessionAsync(string companyName, string subdomain, string adminEmail, SubscriptionTier tier, BillingInterval interval, CancellationToken ct = default);
        Task<IReadOnlyList<InvoiceDto>> GetRecentInvoicesAsync(int tenantId, int max = 10, CancellationToken ct = default);
        Task<bool> PauseSubscriptionAsync(int tenantId, CancellationToken ct = default);
        Task<bool> ResumeSubscriptionAsync(int tenantId, CancellationToken ct = default);

        /// <summary>
        /// Gets the platform fee rate for a tenant based on their subscription tier.
        /// Returns the rate as a decimal (e.g., 0.015 = 1.5%)
        /// </summary>
        Task<decimal> GetPlatformFeeRateAsync(int tenantId, CancellationToken ct = default);

        /// <summary>
        /// Gets the platform fee rate for a subscription tier without tenant lookup.
        /// Returns the rate as a decimal (e.g., 0.015 = 1.5%)
        /// </summary>
        decimal GetPlatformFeeRate(SubscriptionTier? tier);
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

        public async Task<IReadOnlyList<InvoiceDto>> GetRecentInvoicesAsync(int tenantId, int max = 10, CancellationToken ct = default) {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.StripeCustomerId)) return Array.Empty<InvoiceDto>();
            var client = _clientFactory.CreateClient();
            var stripeInvoiceService = new Stripe.InvoiceService(client);
            var options = new InvoiceListOptions { Limit = max, Customer = tenant.StripeCustomerId };
            var list = await stripeInvoiceService.ListAsync(options, null, ct);
            var results = new List<InvoiceDto>(list.Data.Count);
            foreach (var i in list.Data) {
                long amount = 0L;
                try {
                    if (i.AmountPaid != null && i.AmountPaid > 0) amount = Convert.ToInt64(i.AmountPaid);
                    else if (i.AmountDue != null && i.AmountDue > 0) amount = Convert.ToInt64(i.AmountDue);
                } catch { amount = 0L; }

                DateTime created;
                try {
                    object createdObj = (object)i.Created!;
                    if (createdObj is long l) created = DateTimeOffset.FromUnixTimeSeconds(l).UtcDateTime;
                    else if (createdObj is DateTime dt) created = dt.ToUniversalTime();
                    else created = DateTime.UtcNow;
                } catch {
                    created = DateTime.UtcNow;
                }

                results.Add(new InvoiceDto(i.Id, amount, i.Currency ?? "USD", created, i.Status ?? "unknown", i.HostedInvoiceUrl));
            }
            return results;
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

            var sessionOptions = new SessionCreateOptions {
                Mode = "subscription",
                Customer = tenant.StripeCustomerId,
                SuccessUrl = (_urlOptions.SuccessUrl ?? "https://localhost:5001/billing/success") + ( _urlOptions.SuccessUrl?.Contains("?") == true ? "&" : "?" ) + "tenantId=" + tenant.Id + "&token=" + tenant.SignupToken + "&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _urlOptions.CancelUrl ?? "https://localhost:5001/pricing",
                LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
                // Enable promo/coupon codes at checkout
                AllowPromotionCodes = _stripeOptions.AllowPromotionCodes,
                Metadata = new Dictionary<string, string> { ["tenant_id"] = tenant.Id.ToString(), ["tier"] = tier.ToString(), ["interval"] = interval.ToString(), ["admin_email"] = tenant.PendingOwnerEmail ?? string.Empty }
            };

            // Enable automatic tax calculation if configured
            if (_stripeOptions.EnableAutomaticTax) {
                sessionOptions.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };
            }

            var session = await sessionService.CreateAsync(sessionOptions, null, ct);
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

        public async Task<string> CreateDeferredTenantCheckoutSessionAsync(string companyName, string subdomain, string adminEmail, SubscriptionTier tier, BillingInterval interval, CancellationToken ct = default) {
            var priceId = _stripeOptions.GetPriceId(tier, interval);
            if (string.IsNullOrWhiteSpace(priceId)) throw new InvalidOperationException($"No Stripe price id configured for {tier} {interval}");
            var client = _clientFactory.CreateClient();
            var sessionService = new SessionService(client);

            // Redirect directly to tenant homepage (root path) after successful checkout.
            // The tenant is created via webhook; homepage will subsequently allow owner setup via email link.
            var successBase = _urlOptions.SuccessUrl ?? "https://localhost:7056"; // fallback
            
            // Build success URL targeting subdomain root
            try {
                var successUri = new Uri(successBase);
                var host = successUri.Host;
                
                // Skip subdomain manipulation for localhost
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.StartsWith("127.0.0.1")) {
                    // For localhost, just use the base URL as-is (subdomain routing handled by middleware)
                    successBase = $"{successUri.Scheme}://{host}:{successUri.Port}";
                } else {
                    // Production: form subdomain.fullroot (strip any leading www from base)
                    var hostParts = host.Split('.');
                    if (hostParts.Length >= 2) {
                        var rootDomain = string.Join('.', hostParts.Skip(hostParts.Length - 2));
                        successBase = $"{successUri.Scheme}://{subdomain}.{rootDomain}";
                    }
                }
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Error manipulating success URL for subdomain {Subdomain}; using default", subdomain);
            }
            
            var successUrl = successBase + "/?deferred=1&session_id={CHECKOUT_SESSION_ID}";

            var sessionOptions = new SessionCreateOptions {
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = _urlOptions.CancelUrl ?? "https://localhost:7056/tenant/signup?canceled=1",
                LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
                CustomerEmail = adminEmail,
                // Enable promo/coupon codes at checkout
                AllowPromotionCodes = _stripeOptions.AllowPromotionCodes,
                // For trials: "if_required" allows checkout without payment method during trial
                // "always" (default) requires payment method upfront
                PaymentMethodCollection = _stripeOptions.TrialPeriodDays > 0 ? "if_required" : "always",
                Metadata = new Dictionary<string, string> {
                    ["company_name"] = companyName,
                    ["subdomain"] = subdomain,
                    ["admin_email"] = adminEmail,
                    ["tier"] = tier.ToString(),
                    ["interval"] = interval.ToString(),
                    ["deferred_tenant"] = "true"
                },
                SubscriptionData = new SessionSubscriptionDataOptions {
                    // Add free trial period if configured
                    TrialPeriodDays = _stripeOptions.TrialPeriodDays > 0 ? _stripeOptions.TrialPeriodDays : null,
                    Metadata = new Dictionary<string, string> {
                        ["company_name"] = companyName,
                        ["subdomain"] = subdomain,
                        ["admin_email"] = adminEmail,
                        ["tier"] = tier.ToString(),
                        ["interval"] = interval.ToString(),
                        ["deferred_tenant"] = "true"
                    }
                }
            };

            // Enable automatic tax calculation if configured
            if (_stripeOptions.EnableAutomaticTax) {
                sessionOptions.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };
            }

            var session = await sessionService.CreateAsync(sessionOptions, null, ct);
            _logger.LogInformation(
                "Created deferred checkout session {SessionId} for subdomain {Subdomain}. " +
                "TrialDays={TrialDays}, PaymentMethodCollection={PaymentMethodCollection}, Mode={Mode}, PriceId={PriceId}",
                session.Id, subdomain, _stripeOptions.TrialPeriodDays, 
                sessionOptions.PaymentMethodCollection, sessionOptions.Mode, priceId);
            return session.Url;
        }

        /// <summary>
        /// Pauses a subscription at the end of the current billing period.
        /// The customer retains access until the period ends, then enters a paused state.
        /// </summary>
        public async Task<bool> PauseSubscriptionAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId)) {
                _logger.LogWarning("Cannot pause subscription: tenant {TenantId} not found or has no subscription", tenantId);
                return false;
            }

            try {
                var client = _clientFactory.CreateClient();
                var subscriptionService = new SubscriptionService(client);

                // Pause collection - stops billing but keeps subscription active
                var updateOptions = new SubscriptionUpdateOptions {
                    PauseCollection = new SubscriptionPauseCollectionOptions {
                        Behavior = "mark_uncollectible" // or "keep_as_draft" or "void"
                    }
                };

                await subscriptionService.UpdateAsync(tenant.StripeSubscriptionId, updateOptions, cancellationToken: ct);

                tenant.SubscriptionStatus = SubscriptionStatus.Paused;
                tenant.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Paused subscription {SubscriptionId} for tenant {TenantId}", tenant.StripeSubscriptionId, tenantId);
                return true;
            } catch (StripeException ex) {
                _logger.LogError(ex, "Failed to pause subscription for tenant {TenantId}: {Message}", tenantId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Resumes a paused subscription. Billing will resume at the next billing cycle.
        /// </summary>
        public async Task<bool> ResumeSubscriptionAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId)) {
                _logger.LogWarning("Cannot resume subscription: tenant {TenantId} not found or has no subscription", tenantId);
                return false;
            }

            try {
                var client = _clientFactory.CreateClient();
                var subscriptionService = new SubscriptionService(client);

                // Resume the paused subscription
                // Note: ResumeAsync is used for subscriptions paused via pause_collection
                var subscription = await subscriptionService.ResumeAsync(tenant.StripeSubscriptionId, new SubscriptionResumeOptions {
                    BillingCycleAnchor = SubscriptionBillingCycleAnchor.Now
                }, cancellationToken: ct);

                tenant.SubscriptionStatus = SubscriptionStatus.Active;
                tenant.IsActive = true;
                tenant.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Resumed subscription {SubscriptionId} for tenant {TenantId}", tenant.StripeSubscriptionId, tenantId);
                return true;
            } catch (StripeException ex) {
                _logger.LogError(ex, "Failed to resume subscription for tenant {TenantId}: {Message}", tenantId, ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<decimal> GetPlatformFeeRateAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) {
                _logger.LogWarning("Tenant {TenantId} not found, returning default Pro rate", tenantId);
                return SubscriptionTierDefaults.ProPlatformFeeRate;
            }
            return SubscriptionTierDefaults.GetPlatformFeeRate(tenant);
        }

        /// <inheritdoc />
        public decimal GetPlatformFeeRate(SubscriptionTier? tier) {
            return SubscriptionTierDefaults.GetPlatformFeeRate(tier);
        }
    }
}
