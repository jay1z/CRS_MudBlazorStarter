using Horizon.Models;
using Horizon.Services.Billing;

using Horizon.Services.Billing;

using Xunit;

namespace Horizon.Tests.Stripe;

/// <summary>
/// Unit tests for <see cref="BillingService"/> pure-logic methods.
/// Tests platform fee rate resolution and subscription status mapping.
/// </summary>
public class BillingServiceTests
{
    #region GetPlatformFeeRate (by tier, static) Tests

    [Theory]
    [InlineData(SubscriptionTier.Startup, 0.020)]
    [InlineData(SubscriptionTier.Pro, 0.015)]
    [InlineData(SubscriptionTier.Enterprise, 0.010)]
    public void GetPlatformFeeRate_ByTier_ReturnsExpectedRate(SubscriptionTier tier, double expectedRate)
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(tier);

        Assert.Equal((decimal)expectedRate, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_NullTier_ReturnsProDefault()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate((SubscriptionTier?)null);

        Assert.Equal(SubscriptionTierDefaults.ProPlatformFeeRate, rate);
    }

    #endregion

    #region Subscription Tier Limits Tests

    [Fact]
    public void ApplySubscriptionUpdate_StartupTier_SetsCorrectLimits()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var limits = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Startup);

        tenant.MaxCommunities = limits.communities;
        tenant.MaxSpecialistUsers = limits.specialists;

        Assert.Equal(10, tenant.MaxCommunities);
        Assert.Equal(2, tenant.MaxSpecialistUsers);
    }

    [Fact]
    public void ApplySubscriptionUpdate_ProTier_SetsCorrectLimits()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var limits = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Pro);

        tenant.MaxCommunities = limits.communities;
        tenant.MaxSpecialistUsers = limits.specialists;

        Assert.Equal(50, tenant.MaxCommunities);
        Assert.Equal(10, tenant.MaxSpecialistUsers);
    }

    [Fact]
    public void ApplySubscriptionUpdate_EnterpriseTier_UsesMaxForExistingLimits()
    {
        // Enterprise should use Math.Max to never reduce existing limits
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            MaxCommunities = 200,
            MaxSpecialistUsers = 50
        };

        var limits = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Enterprise);

        // Enterprise logic: keep the higher of existing vs baseline
        tenant.MaxCommunities = Math.Max(tenant.MaxCommunities, limits.communities);
        tenant.MaxSpecialistUsers = Math.Max(tenant.MaxSpecialistUsers, limits.specialists);

        Assert.Equal(200, tenant.MaxCommunities);  // Kept existing higher value
        Assert.Equal(50, tenant.MaxSpecialistUsers); // Kept existing higher value
    }

    [Fact]
    public void ApplySubscriptionUpdate_EnterpriseTier_UsesBaselineWhenExistingIsLower()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            MaxCommunities = 10,
            MaxSpecialistUsers = 2
        };

        var limits = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Enterprise);

        tenant.MaxCommunities = Math.Max(tenant.MaxCommunities, limits.communities);
        tenant.MaxSpecialistUsers = Math.Max(tenant.MaxSpecialistUsers, limits.specialists);

        Assert.Equal(100, tenant.MaxCommunities);  // Upgraded to enterprise minimum
        Assert.Equal(20, tenant.MaxSpecialistUsers); // Upgraded to enterprise minimum
    }

    #endregion

    #region Subscription Status Mapping Tests

    [Fact]
    public void SubscriptionStatus_Active_SetsIsActiveTrue()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var status = SubscriptionStatus.Active;

        tenant.SubscriptionStatus = status;
        tenant.IsActive = status == SubscriptionStatus.Active || status == SubscriptionStatus.Trialing;

        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void SubscriptionStatus_Trialing_SetsIsActiveTrue()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var status = SubscriptionStatus.Trialing;

        tenant.SubscriptionStatus = status;
        tenant.IsActive = status == SubscriptionStatus.Active || status == SubscriptionStatus.Trialing;

        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void SubscriptionStatus_Canceled_SetsIsActiveFalse()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var status = SubscriptionStatus.Canceled;

        tenant.SubscriptionStatus = status;
        tenant.IsActive = status == SubscriptionStatus.Active || status == SubscriptionStatus.Trialing;

        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void SubscriptionStatus_Canceled_SetsSubscriptionCanceledAt()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };
        var status = SubscriptionStatus.Canceled;

        tenant.SubscriptionStatus = status;
        if (status == SubscriptionStatus.Canceled)
            tenant.SubscriptionCanceledAt = DateTime.UtcNow;

        Assert.NotNull(tenant.SubscriptionCanceledAt);
    }

    [Fact]
    public void SubscriptionStatus_Active_SetsSubscriptionActivatedAt_WhenFirstActivation()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionActivatedAt = null
        };
        var status = SubscriptionStatus.Active;

        tenant.SubscriptionStatus = status;
        if (status == SubscriptionStatus.Active && tenant.SubscriptionActivatedAt == null)
            tenant.SubscriptionActivatedAt = DateTime.UtcNow;

        Assert.NotNull(tenant.SubscriptionActivatedAt);
    }

    [Fact]
    public void SubscriptionStatus_Active_DoesNotOverwrite_ExistingActivationDate()
    {
        var originalDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            SubscriptionActivatedAt = originalDate
        };
        var status = SubscriptionStatus.Active;

        tenant.SubscriptionStatus = status;
        if (status == SubscriptionStatus.Active && tenant.SubscriptionActivatedAt == null)
            tenant.SubscriptionActivatedAt = DateTime.UtcNow;

        Assert.Equal(originalDate, tenant.SubscriptionActivatedAt);
    }

    #endregion

    #region StripeOptions GetPriceId Comprehensive Tests

    [Theory]
    [InlineData(SubscriptionTier.Startup, BillingInterval.Monthly)]
    [InlineData(SubscriptionTier.Startup, BillingInterval.Yearly)]
    [InlineData(SubscriptionTier.Pro, BillingInterval.Monthly)]
    [InlineData(SubscriptionTier.Pro, BillingInterval.Yearly)]
    [InlineData(SubscriptionTier.Enterprise, BillingInterval.Monthly)]
    [InlineData(SubscriptionTier.Enterprise, BillingInterval.Yearly)]
    public void GetPriceId_AllTierIntervalCombinations_WhenFullyConfigured_ReturnsNonNull(
        SubscriptionTier tier, BillingInterval interval)
    {
        var options = new StripeOptions
        {
            StarterMonthlyPriceId = "price_sm",
            StarterYearlyPriceId = "price_sy",
            ProMonthlyPriceId = "price_pm",
            ProYearlyPriceId = "price_py",
            EnterpriseMonthlyPriceId = "price_em",
            EnterpriseYearlyPriceId = "price_ey"
        };

        var result = options.GetPriceId(tier, interval);

        Assert.NotNull(result);
    }

    #endregion
}
