using Horizon.Models;

using Horizon.Services.Billing;

using Xunit;

namespace Horizon.Tests.Stripe;

/// <summary>
/// Unit tests for <see cref="StripeOptions"/> price ID resolution
/// and <see cref="SubscriptionTierDefaults"/> platform fee calculations.
/// </summary>
public class StripeOptionsTests
{
    #region GetPriceId Tests

    [Fact]
    public void GetPriceId_StartupMonthly_ReturnsStarterMonthlyPriceId()
    {
        var options = new StripeOptions { StarterMonthlyPriceId = "price_starter_monthly" };

        var result = options.GetPriceId(SubscriptionTier.Startup, BillingInterval.Monthly);

        Assert.Equal("price_starter_monthly", result);
    }

    [Fact]
    public void GetPriceId_StartupMonthly_FallsBackToStartupPriceId()
    {
        var options = new StripeOptions
        {
            StarterMonthlyPriceId = null,
            StartupPriceId = "price_startup_legacy"
        };

        var result = options.GetPriceId(SubscriptionTier.Startup, BillingInterval.Monthly);

        Assert.Equal("price_startup_legacy", result);
    }

    [Fact]
    public void GetPriceId_StartupYearly_ReturnsStarterYearlyPriceId()
    {
        var options = new StripeOptions { StarterYearlyPriceId = "price_starter_yearly" };

        var result = options.GetPriceId(SubscriptionTier.Startup, BillingInterval.Yearly);

        Assert.Equal("price_starter_yearly", result);
    }

    [Fact]
    public void GetPriceId_StartupYearly_ReturnsNull_WhenNotConfigured()
    {
        var options = new StripeOptions();

        var result = options.GetPriceId(SubscriptionTier.Startup, BillingInterval.Yearly);

        Assert.Null(result);
    }

    [Fact]
    public void GetPriceId_ProMonthly_ReturnsProMonthlyPriceId()
    {
        var options = new StripeOptions { ProMonthlyPriceId = "price_pro_monthly" };

        var result = options.GetPriceId(SubscriptionTier.Pro, BillingInterval.Monthly);

        Assert.Equal("price_pro_monthly", result);
    }

    [Fact]
    public void GetPriceId_ProMonthly_FallsBackToProPriceId()
    {
        var options = new StripeOptions
        {
            ProMonthlyPriceId = null,
            ProPriceId = "price_pro_legacy"
        };

        var result = options.GetPriceId(SubscriptionTier.Pro, BillingInterval.Monthly);

        Assert.Equal("price_pro_legacy", result);
    }

    [Fact]
    public void GetPriceId_ProYearly_ReturnsProYearlyPriceId()
    {
        var options = new StripeOptions { ProYearlyPriceId = "price_pro_yearly" };

        var result = options.GetPriceId(SubscriptionTier.Pro, BillingInterval.Yearly);

        Assert.Equal("price_pro_yearly", result);
    }

    [Fact]
    public void GetPriceId_EnterpriseMonthly_ReturnsEnterpriseMonthlyPriceId()
    {
        var options = new StripeOptions { EnterpriseMonthlyPriceId = "price_enterprise_monthly" };

        var result = options.GetPriceId(SubscriptionTier.Enterprise, BillingInterval.Monthly);

        Assert.Equal("price_enterprise_monthly", result);
    }

    [Fact]
    public void GetPriceId_EnterpriseMonthly_FallsBackToEnterprisePriceId()
    {
        var options = new StripeOptions
        {
            EnterpriseMonthlyPriceId = null,
            EnterprisePriceId = "price_enterprise_legacy"
        };

        var result = options.GetPriceId(SubscriptionTier.Enterprise, BillingInterval.Monthly);

        Assert.Equal("price_enterprise_legacy", result);
    }

    [Fact]
    public void GetPriceId_EnterpriseYearly_ReturnsEnterpriseYearlyPriceId()
    {
        var options = new StripeOptions { EnterpriseYearlyPriceId = "price_enterprise_yearly" };

        var result = options.GetPriceId(SubscriptionTier.Enterprise, BillingInterval.Yearly);

        Assert.Equal("price_enterprise_yearly", result);
    }

    [Fact]
    public void GetPriceId_UnknownTier_ReturnsNull()
    {
        var options = new StripeOptions
        {
            StarterMonthlyPriceId = "price_starter",
            ProMonthlyPriceId = "price_pro"
        };

        var result = options.GetPriceId((SubscriptionTier)99, BillingInterval.Monthly);

        Assert.Null(result);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void StripeOptions_DefaultTrialPeriodDays_Is14()
    {
        var options = new StripeOptions();

        Assert.Equal(14, options.TrialPeriodDays);
    }

    [Fact]
    public void StripeOptions_DefaultAllowPromotionCodes_IsTrue()
    {
        var options = new StripeOptions();

        Assert.True(options.AllowPromotionCodes);
    }

    [Fact]
    public void StripeOptions_DefaultEnableAutomaticTax_IsFalse()
    {
        var options = new StripeOptions();

        Assert.False(options.EnableAutomaticTax);
    }

    #endregion
}

/// <summary>
/// Unit tests for <see cref="SubscriptionTierDefaults"/> platform fee rates and tier limits.
/// </summary>
public class SubscriptionTierDefaultsTests
{
    #region GetLimits Tests

    [Fact]
    public void GetLimits_Startup_ReturnsCorrectLimits()
    {
        var (communities, specialists) = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Startup);

        Assert.Equal(SubscriptionTierDefaults.StartupMaxCommunities, communities);
        Assert.Equal(SubscriptionTierDefaults.StartupMaxSpecialistUsers, specialists);
    }

    [Fact]
    public void GetLimits_Pro_ReturnsCorrectLimits()
    {
        var (communities, specialists) = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Pro);

        Assert.Equal(SubscriptionTierDefaults.ProMaxCommunities, communities);
        Assert.Equal(SubscriptionTierDefaults.ProMaxSpecialistUsers, specialists);
    }

    [Fact]
    public void GetLimits_Enterprise_ReturnsMinimumLimits()
    {
        var (communities, specialists) = SubscriptionTierDefaults.GetLimits(SubscriptionTier.Enterprise);

        Assert.Equal(SubscriptionTierDefaults.EnterpriseMinCommunities, communities);
        Assert.Equal(SubscriptionTierDefaults.EnterpriseMinSpecialistUsers, specialists);
    }

    [Fact]
    public void GetLimits_UnknownTier_ReturnsZero()
    {
        var (communities, specialists) = SubscriptionTierDefaults.GetLimits((SubscriptionTier)99);

        Assert.Equal(0, communities);
        Assert.Equal(0, specialists);
    }

    #endregion

    #region GetPlatformFeeRate (by tier) Tests

    [Fact]
    public void GetPlatformFeeRate_Startup_Returns2Percent()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(SubscriptionTier.Startup);

        Assert.Equal(0.020m, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_Pro_Returns1Point5Percent()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(SubscriptionTier.Pro);

        Assert.Equal(0.015m, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_Enterprise_Returns1Percent()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(SubscriptionTier.Enterprise);

        Assert.Equal(0.010m, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_Null_DefaultsToProRate()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate((SubscriptionTier?)null);

        Assert.Equal(SubscriptionTierDefaults.ProPlatformFeeRate, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_UnknownTier_DefaultsToProRate()
    {
        var rate = SubscriptionTierDefaults.GetPlatformFeeRate((SubscriptionTier)99);

        Assert.Equal(SubscriptionTierDefaults.ProPlatformFeeRate, rate);
    }

    #endregion

    #region GetPlatformFeeRate (by tenant) Tests

    [Fact]
    public void GetPlatformFeeRate_Tenant_UsesOverrideWhenSet()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Pro,
            PlatformFeeRateOverride = 0.005m // Custom 0.5%
        };

        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);

        Assert.Equal(0.005m, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_Tenant_UsesTierWhenNoOverride()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Enterprise,
            PlatformFeeRateOverride = null
        };

        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);

        Assert.Equal(SubscriptionTierDefaults.EnterprisePlatformFeeRate, rate);
    }

    [Fact]
    public void GetPlatformFeeRate_Tenant_NullTier_DefaultsToProRate()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = null,
            PlatformFeeRateOverride = null
        };

        var rate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);

        Assert.Equal(SubscriptionTierDefaults.ProPlatformFeeRate, rate);
    }

    #endregion

    #region Fee Rate Constants Tests

    [Fact]
    public void PlatformFeeRates_AreInDescendingOrder()
    {
        Assert.True(SubscriptionTierDefaults.StartupPlatformFeeRate > SubscriptionTierDefaults.ProPlatformFeeRate);
        Assert.True(SubscriptionTierDefaults.ProPlatformFeeRate > SubscriptionTierDefaults.EnterprisePlatformFeeRate);
    }

    [Fact]
    public void PlatformFeeRates_AreWithinReasonableRange()
    {
        Assert.InRange(SubscriptionTierDefaults.StartupPlatformFeeRate, 0.001m, 0.10m);
        Assert.InRange(SubscriptionTierDefaults.ProPlatformFeeRate, 0.001m, 0.10m);
        Assert.InRange(SubscriptionTierDefaults.EnterprisePlatformFeeRate, 0.001m, 0.10m);
    }

    #endregion
}
