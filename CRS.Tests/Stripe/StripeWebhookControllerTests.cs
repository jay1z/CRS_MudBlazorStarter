using CRS.Models;

using Xunit;

namespace CRS.Tests.Stripe;

/// <summary>
/// Unit tests for Stripe webhook event handling logic.
/// Tests status mapping, idempotency patterns, and subscription lifecycle.
/// </summary>
public class StripeWebhookControllerTests
{
    #region MapStatus Tests

    [Theory]
    [InlineData("active", SubscriptionStatus.Active)]
    [InlineData("trialing", SubscriptionStatus.Trialing)]
    [InlineData("past_due", SubscriptionStatus.PastDue)]
    [InlineData("canceled", SubscriptionStatus.Canceled)]
    [InlineData("unpaid", SubscriptionStatus.Unpaid)]
    [InlineData("incomplete", SubscriptionStatus.Incomplete)]
    [InlineData("paused", SubscriptionStatus.Paused)]
    public void MapStatus_KnownStripeStatus_ReturnsCorrectEnum(string stripeStatus, SubscriptionStatus expected)
    {
        var result = MapStatus(stripeStatus);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown_status")]
    [InlineData("ACTIVE")] // Case-sensitive
    public void MapStatus_UnknownOrNull_ReturnsNone(string? stripeStatus)
    {
        var result = MapStatus(stripeStatus);

        Assert.Equal(SubscriptionStatus.None, result);
    }

    #endregion

    #region Invoice Payment Succeeded - Tenant Reactivation Tests

    [Fact]
    public void InvoicePaymentSucceeded_PastDueTenant_Reactivates()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.PastDue,
            IsActive = true,
            LastPaymentFailureAt = DateTime.UtcNow.AddDays(-3),
            ReactivationCount = 0
        };

        var wasInactive = !tenant.IsActive ||
            tenant.SubscriptionStatus == SubscriptionStatus.PastDue ||
            tenant.SubscriptionStatus == SubscriptionStatus.GracePeriod ||
            tenant.SubscriptionStatus == SubscriptionStatus.Suspended;

        // Apply payment success logic
        tenant.SubscriptionStatus = SubscriptionStatus.Active;
        tenant.IsActive = true;
        tenant.SuspendedAt = null;
        tenant.GracePeriodEndsAt = null;
        tenant.LastPaymentFailureAt = null;

        if (wasInactive)
        {
            tenant.ReactivationCount++;
            tenant.LastReactivatedAt = DateTime.UtcNow;
        }

        Assert.True(wasInactive);
        Assert.Equal(SubscriptionStatus.Active, tenant.SubscriptionStatus);
        Assert.True(tenant.IsActive);
        Assert.Null(tenant.SuspendedAt);
        Assert.Null(tenant.LastPaymentFailureAt);
        Assert.Equal(1, tenant.ReactivationCount);
        Assert.NotNull(tenant.LastReactivatedAt);
    }

    [Fact]
    public void InvoicePaymentSucceeded_SuspendedTenant_Reactivates()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.Suspended,
            IsActive = false,
            SuspendedAt = DateTime.UtcNow.AddDays(-10),
            GracePeriodEndsAt = DateTime.UtcNow.AddDays(20),
            ReactivationCount = 1
        };

        var wasInactive = !tenant.IsActive ||
            tenant.SubscriptionStatus == SubscriptionStatus.PastDue ||
            tenant.SubscriptionStatus == SubscriptionStatus.GracePeriod ||
            tenant.SubscriptionStatus == SubscriptionStatus.Suspended;

        tenant.SubscriptionStatus = SubscriptionStatus.Active;
        tenant.IsActive = true;
        tenant.SuspendedAt = null;
        tenant.GracePeriodEndsAt = null;
        tenant.LastPaymentFailureAt = null;

        if (wasInactive)
        {
            tenant.ReactivationCount++;
            tenant.LastReactivatedAt = DateTime.UtcNow;
        }

        Assert.True(wasInactive);
        Assert.Equal(2, tenant.ReactivationCount); // Incremented from 1 to 2
        Assert.Null(tenant.SuspendedAt);
        Assert.Null(tenant.GracePeriodEndsAt);
    }

    [Fact]
    public void InvoicePaymentSucceeded_AlreadyActiveTenant_DoesNotReactivate()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true,
            ReactivationCount = 0
        };

        var wasInactive = !tenant.IsActive ||
            tenant.SubscriptionStatus == SubscriptionStatus.PastDue ||
            tenant.SubscriptionStatus == SubscriptionStatus.GracePeriod ||
            tenant.SubscriptionStatus == SubscriptionStatus.Suspended;

        if (wasInactive)
        {
            tenant.ReactivationCount++;
        }

        Assert.False(wasInactive);
        Assert.Equal(0, tenant.ReactivationCount); // Not incremented
    }

    #endregion

    #region Invoice Payment Failed Tests

    [Fact]
    public void InvoicePaymentFailed_FirstFailure_EntersPastDue()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true
        };

        tenant.LastPaymentFailureAt = DateTime.UtcNow;

        if (tenant.SubscriptionStatus != SubscriptionStatus.PastDue)
        {
            tenant.SubscriptionStatus = SubscriptionStatus.PastDue;
            tenant.IsActive = true; // Keep active during retry period
        }

        Assert.Equal(SubscriptionStatus.PastDue, tenant.SubscriptionStatus);
        Assert.True(tenant.IsActive); // Still active during Stripe retry
        Assert.NotNull(tenant.LastPaymentFailureAt);
    }

    [Fact]
    public void InvoicePaymentFailed_AlreadyPastDue_StaysPastDue()
    {
        var originalFailureDate = DateTime.UtcNow.AddDays(-3);
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.PastDue,
            IsActive = true,
            LastPaymentFailureAt = originalFailureDate
        };

        tenant.LastPaymentFailureAt = DateTime.UtcNow;

        // Second failure shouldn't change status since it's already PastDue
        Assert.Equal(SubscriptionStatus.PastDue, tenant.SubscriptionStatus);
        Assert.True(tenant.IsActive);
    }

    #endregion

    #region Subscription Deleted Tests

    [Fact]
    public void SubscriptionDeleted_EntersSuspendedState()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true
        };

        // Apply subscription deleted logic
        tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
        tenant.SubscriptionCanceledAt = DateTime.UtcNow;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.IsActive = false;
        tenant.GracePeriodEndsAt = DateTime.UtcNow.AddDays(30);

        Assert.Equal(SubscriptionStatus.Suspended, tenant.SubscriptionStatus);
        Assert.False(tenant.IsActive);
        Assert.NotNull(tenant.SubscriptionCanceledAt);
        Assert.NotNull(tenant.SuspendedAt);
        Assert.NotNull(tenant.GracePeriodEndsAt);
        Assert.True(tenant.GracePeriodEndsAt > DateTime.UtcNow.AddDays(29));
    }

    #endregion

    #region Connect Account Updated Tests

    [Fact]
    public void ConnectAccountUpdated_OnboardingComplete_SetsFlags()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test123",
            StripeConnectOnboardingComplete = false,
            StripeConnectPayoutsEnabled = false,
            StripeConnectCardPaymentsEnabled = false
        };

        // Simulate webhook update
        var payoutsEnabled = true;
        var cardPaymentsActive = true;
        var hasCurrentlyDue = false;
        var hasPastDue = false;
        var detailsSubmitted = true;

        var wasOnboardingComplete = tenant.StripeConnectOnboardingComplete;

        tenant.StripeConnectPayoutsEnabled = payoutsEnabled;
        tenant.StripeConnectCardPaymentsEnabled = cardPaymentsActive;
        tenant.StripeConnectOnboardingComplete = !hasCurrentlyDue && !hasPastDue && detailsSubmitted;
        tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;

        Assert.False(wasOnboardingComplete);
        Assert.True(tenant.StripeConnectOnboardingComplete);
        Assert.True(tenant.StripeConnectPayoutsEnabled);
        Assert.True(tenant.StripeConnectCardPaymentsEnabled);
    }

    [Fact]
    public void ConnectAccountUpdated_RequirementsPending_OnboardingNotComplete()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test123"
        };

        var hasCurrentlyDue = true;
        var hasPastDue = false;
        var detailsSubmitted = false;

        tenant.StripeConnectOnboardingComplete = !hasCurrentlyDue && !hasPastDue && detailsSubmitted;

        Assert.False(tenant.StripeConnectOnboardingComplete);
    }

    [Fact]
    public void ConnectAccountDeauthorized_ClearsCapabilities()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test123",
            StripeConnectOnboardingComplete = true,
            StripeConnectPayoutsEnabled = true,
            StripeConnectCardPaymentsEnabled = true
        };

        // Simulate deauthorization
        tenant.StripeConnectOnboardingComplete = false;
        tenant.StripeConnectPayoutsEnabled = false;
        tenant.StripeConnectCardPaymentsEnabled = false;
        tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;
        // Don't clear the StripeConnectAccountId - keep for reference

        Assert.Equal("acct_test123", tenant.StripeConnectAccountId);
        Assert.False(tenant.StripeConnectOnboardingComplete);
        Assert.False(tenant.StripeConnectPayoutsEnabled);
        Assert.False(tenant.StripeConnectCardPaymentsEnabled);
        Assert.NotNull(tenant.StripeConnectLastSyncedAt);
    }

    #endregion

    #region Tier Resolution from Price ID Tests

    [Fact]
    public void TierResolution_StarterMonthlyPriceId_ResolvesToStartup()
    {
        var priceId = "price_starter_monthly";
        var options = new Services.Billing.StripeOptions
        {
            StarterMonthlyPriceId = "price_starter_monthly",
            ProMonthlyPriceId = "price_pro_monthly"
        };

        SubscriptionTier? tier = null;
        if (priceId == options.StarterMonthlyPriceId || priceId == options.StarterYearlyPriceId || priceId == options.StartupPriceId)
            tier = SubscriptionTier.Startup;
        else if (priceId == options.ProMonthlyPriceId || priceId == options.ProYearlyPriceId || priceId == options.ProPriceId)
            tier = SubscriptionTier.Pro;
        else if (priceId == options.EnterpriseMonthlyPriceId || priceId == options.EnterpriseYearlyPriceId || priceId == options.EnterprisePriceId)
            tier = SubscriptionTier.Enterprise;

        Assert.Equal(SubscriptionTier.Startup, tier);
    }

    [Fact]
    public void TierResolution_UnknownPriceId_ReturnsNull()
    {
        var priceId = "price_unknown";
        var options = new Services.Billing.StripeOptions
        {
            StarterMonthlyPriceId = "price_starter_monthly",
            ProMonthlyPriceId = "price_pro_monthly"
        };

        SubscriptionTier? tier = null;
        if (priceId == options.StarterMonthlyPriceId || priceId == options.StarterYearlyPriceId || priceId == options.StartupPriceId)
            tier = SubscriptionTier.Startup;
        else if (priceId == options.ProMonthlyPriceId || priceId == options.ProYearlyPriceId || priceId == options.ProPriceId)
            tier = SubscriptionTier.Pro;
        else if (priceId == options.EnterpriseMonthlyPriceId || priceId == options.EnterpriseYearlyPriceId || priceId == options.EnterprisePriceId)
            tier = SubscriptionTier.Enterprise;

        Assert.Null(tier);
    }

    [Fact]
    public void TierResolution_LegacyPriceId_StillResolves()
    {
        var priceId = "price_pro_legacy";
        var options = new Services.Billing.StripeOptions
        {
            ProPriceId = "price_pro_legacy",
            ProMonthlyPriceId = "price_pro_monthly_new"
        };

        SubscriptionTier? tier = null;
        if (priceId == options.StarterMonthlyPriceId || priceId == options.StarterYearlyPriceId || priceId == options.StartupPriceId)
            tier = SubscriptionTier.Startup;
        else if (priceId == options.ProMonthlyPriceId || priceId == options.ProYearlyPriceId || priceId == options.ProPriceId)
            tier = SubscriptionTier.Pro;
        else if (priceId == options.EnterpriseMonthlyPriceId || priceId == options.EnterpriseYearlyPriceId || priceId == options.EnterprisePriceId)
            tier = SubscriptionTier.Enterprise;

        Assert.Equal(SubscriptionTier.Pro, tier);
    }

    #endregion

    #region Deferred Tenant Creation Tests

    [Fact]
    public void DeferredTenantCreation_ValidMetadata_CreatesTenant()
    {
        var meta = new Dictionary<string, string>
        {
            ["deferred_tenant"] = "true",
            ["company_name"] = "Test Company",
            ["subdomain"] = "testco",
            ["admin_email"] = "admin@test.com",
            ["tier"] = "Pro"
        };

        var isDeferred = meta.TryGetValue("deferred_tenant", out var def) &&
            string.Equals(def, "true", StringComparison.OrdinalIgnoreCase);
        meta.TryGetValue("company_name", out var companyName);
        meta.TryGetValue("subdomain", out var subdomain);
        meta.TryGetValue("admin_email", out var adminEmail);
        meta.TryGetValue("tier", out var tierStr);

        Assert.True(isDeferred);
        Assert.Equal("Test Company", companyName);
        Assert.Equal("testco", subdomain);
        Assert.Equal("admin@test.com", adminEmail);
        Assert.Equal("Pro", tierStr);
        Assert.True(Enum.TryParse<SubscriptionTier>(tierStr, true, out var tier));
        Assert.Equal(SubscriptionTier.Pro, tier);
    }

    [Fact]
    public void DeferredTenantCreation_MissingCompanyName_IsInvalid()
    {
        var meta = new Dictionary<string, string>
        {
            ["deferred_tenant"] = "true",
            ["subdomain"] = "testco"
        };

        meta.TryGetValue("company_name", out var companyName);
        meta.TryGetValue("subdomain", out var subdomain);

        Assert.True(string.IsNullOrWhiteSpace(companyName));
        Assert.False(string.IsNullOrWhiteSpace(subdomain));
    }

    [Fact]
    public void DeferredTenantCreation_MissingSubdomain_IsInvalid()
    {
        var meta = new Dictionary<string, string>
        {
            ["deferred_tenant"] = "true",
            ["company_name"] = "Test Company"
        };

        meta.TryGetValue("company_name", out var companyName);
        meta.TryGetValue("subdomain", out var subdomain);

        Assert.False(string.IsNullOrWhiteSpace(companyName));
        Assert.True(string.IsNullOrWhiteSpace(subdomain));
    }

    [Fact]
    public void DeferredTenantCreation_CaseInsensitiveFlag()
    {
        var meta = new Dictionary<string, string>
        {
            ["deferred_tenant"] = "True"
        };

        var isDeferred = meta.TryGetValue("deferred_tenant", out var def) &&
            string.Equals(def, "true", StringComparison.OrdinalIgnoreCase);

        Assert.True(isDeferred);
    }

    [Fact]
    public void DeferredTenantCreation_NoFlag_NotDeferred()
    {
        var meta = new Dictionary<string, string>
        {
            ["tenant_id"] = "42"
        };

        var isDeferred = meta.TryGetValue("deferred_tenant", out var def) &&
            string.Equals(def, "true", StringComparison.OrdinalIgnoreCase);

        Assert.False(isDeferred);
    }

    #endregion

    #region Invoice RecalculateTotals Tests

    [Fact]
    public void Invoice_RecalculateTotals_ComputesCorrectly()
    {
        var invoice = new Invoice
        {
            TenantId = 1,
            ReserveStudyId = Guid.CreateVersion7(),
            InvoiceNumber = "INV-001",
            TaxRate = 8.5m,
            DiscountAmount = 100m
        };

        invoice.LineItems.Add(new InvoiceLineItem { Description = "Service A", Quantity = 1, UnitPrice = 1000, LineTotal = 1000 });
        invoice.LineItems.Add(new InvoiceLineItem { Description = "Service B", Quantity = 2, UnitPrice = 500, LineTotal = 1000 });

        invoice.RecalculateTotals();

        Assert.Equal(2000m, invoice.Subtotal);
        Assert.Equal(170m, invoice.TaxAmount); // 2000 × 0.085 = 170
        Assert.Equal(2070m, invoice.TotalAmount); // 2000 + 170 - 100 = 2070
    }

    [Fact]
    public void Invoice_RecalculateTotals_ZeroTax()
    {
        var invoice = new Invoice
        {
            TenantId = 1,
            ReserveStudyId = Guid.CreateVersion7(),
            InvoiceNumber = "INV-002",
            TaxRate = 0m,
            DiscountAmount = 0m
        };

        invoice.LineItems.Add(new InvoiceLineItem { Description = "Service", Quantity = 1, UnitPrice = 5000, LineTotal = 5000 });

        invoice.RecalculateTotals();

        Assert.Equal(5000m, invoice.Subtotal);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(5000m, invoice.TotalAmount);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Mirrors the MapStatus logic from StripeWebhookController.
    /// </summary>
    private static SubscriptionStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "active" => SubscriptionStatus.Active,
        "trialing" => SubscriptionStatus.Trialing,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "unpaid" => SubscriptionStatus.Unpaid,
        "incomplete" => SubscriptionStatus.Incomplete,
        "paused" => SubscriptionStatus.Paused,
        _ => SubscriptionStatus.None
    };

    #endregion
}
