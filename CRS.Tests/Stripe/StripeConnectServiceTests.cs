using CRS.Models;
using CRS.Services.Billing;

using Xunit;

namespace CRS.Tests.Stripe;

/// <summary>
/// Unit tests for <see cref="StripeConnectStatus"/> record and
/// Stripe Connect-related tenant state transitions.
/// </summary>
public class StripeConnectServiceTests
{
    #region StripeConnectStatus Record Tests

    [Fact]
    public void StripeConnectStatus_NoAccount_DefaultValues()
    {
        var status = new StripeConnectStatus(
            HasConnectAccount: false,
            AccountId: null,
            OnboardingComplete: false,
            PayoutsEnabled: false,
            CardPaymentsEnabled: false,
            RequiresAction: false,
            RequirementsStatus: null,
            LastSynced: null);

        Assert.False(status.HasConnectAccount);
        Assert.Null(status.AccountId);
        Assert.False(status.OnboardingComplete);
        Assert.False(status.PayoutsEnabled);
        Assert.False(status.CardPaymentsEnabled);
        Assert.False(status.RequiresAction);
        Assert.Null(status.RequirementsStatus);
        Assert.Null(status.LastSynced);
    }

    [Fact]
    public void StripeConnectStatus_FullyOnboarded_AllEnabled()
    {
        var syncTime = DateTime.UtcNow;
        var status = new StripeConnectStatus(
            HasConnectAccount: true,
            AccountId: "acct_test123",
            OnboardingComplete: true,
            PayoutsEnabled: true,
            CardPaymentsEnabled: true,
            RequiresAction: false,
            RequirementsStatus: "complete",
            LastSynced: syncTime);

        Assert.True(status.HasConnectAccount);
        Assert.Equal("acct_test123", status.AccountId);
        Assert.True(status.OnboardingComplete);
        Assert.True(status.PayoutsEnabled);
        Assert.True(status.CardPaymentsEnabled);
        Assert.False(status.RequiresAction);
        Assert.Equal("complete", status.RequirementsStatus);
        Assert.Equal(syncTime, status.LastSynced);
    }

    [Fact]
    public void StripeConnectStatus_OnboardingIncomplete_RequiresAction()
    {
        var status = new StripeConnectStatus(
            HasConnectAccount: true,
            AccountId: "acct_test456",
            OnboardingComplete: false,
            PayoutsEnabled: false,
            CardPaymentsEnabled: false,
            RequiresAction: true,
            RequirementsStatus: "currently_due",
            LastSynced: DateTime.UtcNow);

        Assert.True(status.HasConnectAccount);
        Assert.False(status.OnboardingComplete);
        Assert.True(status.RequiresAction);
        Assert.Equal("currently_due", status.RequirementsStatus);
    }

    #endregion

    #region StripeConnectOnboardingResult Record Tests

    [Fact]
    public void StripeConnectOnboardingResult_Success_HasAccountIdAndUrl()
    {
        var result = new StripeConnectOnboardingResult(
            Success: true,
            AccountId: "acct_new123",
            OnboardingUrl: "https://connect.stripe.com/setup/s/test",
            ErrorMessage: null);

        Assert.True(result.Success);
        Assert.Equal("acct_new123", result.AccountId);
        Assert.NotNull(result.OnboardingUrl);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void StripeConnectOnboardingResult_TenantNotFound_ReturnsError()
    {
        var result = new StripeConnectOnboardingResult(
            Success: false,
            AccountId: null,
            OnboardingUrl: null,
            ErrorMessage: "Tenant not found");

        Assert.False(result.Success);
        Assert.Null(result.AccountId);
        Assert.Null(result.OnboardingUrl);
        Assert.Equal("Tenant not found", result.ErrorMessage);
    }

    [Fact]
    public void StripeConnectOnboardingResult_StripeError_ReturnsError()
    {
        var result = new StripeConnectOnboardingResult(
            Success: false,
            AccountId: null,
            OnboardingUrl: null,
            ErrorMessage: "Your account cannot currently make live charges.");

        Assert.False(result.Success);
        Assert.Contains("cannot currently make live charges", result.ErrorMessage);
    }

    #endregion

    #region Tenant Connect State Tests

    [Fact]
    public void Tenant_NewConnectAccount_DefaultsFalse()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };

        Assert.Null(tenant.StripeConnectAccountId);
        Assert.False(tenant.StripeConnectOnboardingComplete);
        Assert.False(tenant.StripeConnectPayoutsEnabled);
        Assert.False(tenant.StripeConnectCardPaymentsEnabled);
        Assert.Null(tenant.StripeConnectCreatedAt);
        Assert.Null(tenant.StripeConnectLastSyncedAt);
    }

    [Fact]
    public void Tenant_ConnectAccountCreated_SetsAccountId()
    {
        var tenant = new Tenant { Name = "Test", Subdomain = "test" };

        tenant.StripeConnectAccountId = "acct_test789";
        tenant.StripeConnectCreatedAt = DateTime.UtcNow;
        tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;

        Assert.Equal("acct_test789", tenant.StripeConnectAccountId);
        Assert.NotNull(tenant.StripeConnectCreatedAt);
        Assert.NotNull(tenant.StripeConnectLastSyncedAt);
    }

    [Fact]
    public void Tenant_ConnectOnboardingComplete_EnablesPaymentCapability()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test789",
            StripeConnectOnboardingComplete = true,
            StripeConnectPayoutsEnabled = true,
            StripeConnectCardPaymentsEnabled = true
        };

        var hasConnectAccount = !string.IsNullOrEmpty(tenant.StripeConnectAccountId)
            && tenant.StripeConnectOnboardingComplete
            && tenant.StripeConnectCardPaymentsEnabled;

        Assert.True(hasConnectAccount);
    }

    [Fact]
    public void Tenant_ConnectOnboardingIncomplete_DisablesPaymentCapability()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test789",
            StripeConnectOnboardingComplete = false, // Not yet complete
            StripeConnectPayoutsEnabled = false,
            StripeConnectCardPaymentsEnabled = false
        };

        var hasConnectAccount = !string.IsNullOrEmpty(tenant.StripeConnectAccountId)
            && tenant.StripeConnectOnboardingComplete
            && tenant.StripeConnectCardPaymentsEnabled;

        Assert.False(hasConnectAccount);
    }

    [Fact]
    public void Tenant_ConnectDeauthorized_ClearsOnboardingButKeepsAccountId()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test789",
            StripeConnectOnboardingComplete = true,
            StripeConnectPayoutsEnabled = true,
            StripeConnectCardPaymentsEnabled = true
        };

        // Simulate deauthorization (from webhook handler)
        tenant.StripeConnectOnboardingComplete = false;
        tenant.StripeConnectPayoutsEnabled = false;
        tenant.StripeConnectCardPaymentsEnabled = false;
        tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;

        // Account ID preserved for reference
        Assert.Equal("acct_test789", tenant.StripeConnectAccountId);
        Assert.False(tenant.StripeConnectOnboardingComplete);
        Assert.False(tenant.StripeConnectPayoutsEnabled);
        Assert.False(tenant.StripeConnectCardPaymentsEnabled);
    }

    #endregion

    #region Application Fee Calculation Tests

    [Theory]
    [InlineData(SubscriptionTier.Startup, 1000.00, 2000)]   // 1000 × 0.020 × 100 = 2000 cents
    [InlineData(SubscriptionTier.Pro, 1000.00, 1500)]        // 1000 × 0.015 × 100 = 1500 cents
    [InlineData(SubscriptionTier.Enterprise, 1000.00, 1000)] // 1000 × 0.010 × 100 = 1000 cents
    public void CalculateApplicationFee_ByTier_ReturnsCorrectCents(
        SubscriptionTier tier, double paymentAmount, long expectedFeeCents)
    {
        var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(tier);
        var feeInCents = (long)Math.Ceiling((decimal)paymentAmount * feeRate * 100);

        Assert.Equal(expectedFeeCents, feeInCents);
    }

    [Fact]
    public void CalculateApplicationFee_WithCustomOverride_UsesOverride()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            Tier = SubscriptionTier.Pro,
            PlatformFeeRateOverride = 0.005m // Custom 0.5%
        };

        var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);
        var feeInCents = (long)Math.Ceiling(5000m * feeRate * 100); // $5000 payment

        Assert.Equal(0.005m, feeRate);
        Assert.Equal(2500, feeInCents); // 5000 × 0.005 × 100 = 2500 cents
    }

    [Fact]
    public void CalculateApplicationFee_SmallAmount_RoundsUp()
    {
        var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(SubscriptionTier.Enterprise);
        var feeInCents = (long)Math.Ceiling(99.99m * feeRate * 100); // $99.99 × 0.01 × 100 = 99.99 cents

        Assert.Equal(100, feeInCents); // Rounds up to 100 cents
    }

    [Fact]
    public void CalculateApplicationFee_ZeroAmount_ReturnsZero()
    {
        var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(SubscriptionTier.Pro);
        var feeInCents = (long)Math.Ceiling(0m * feeRate * 100);

        Assert.Equal(0, feeInCents);
    }

    #endregion

    #region GetAccountStatus Sync Decision Tests

    [Fact]
    public void SyncDecision_NeverSynced_ShouldSync()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test",
            StripeConnectLastSyncedAt = null
        };

        var shouldSync = tenant.StripeConnectLastSyncedAt == null ||
            DateTime.UtcNow - tenant.StripeConnectLastSyncedAt > TimeSpan.FromMinutes(5);

        Assert.True(shouldSync);
    }

    [Fact]
    public void SyncDecision_RecentlySynced_ShouldNotSync()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test",
            StripeConnectLastSyncedAt = DateTime.UtcNow.AddMinutes(-2) // 2 minutes ago
        };

        var shouldSync = tenant.StripeConnectLastSyncedAt == null ||
            DateTime.UtcNow - tenant.StripeConnectLastSyncedAt > TimeSpan.FromMinutes(5);

        Assert.False(shouldSync);
    }

    [Fact]
    public void SyncDecision_SyncedOverFiveMinutesAgo_ShouldSync()
    {
        var tenant = new Tenant
        {
            Name = "Test",
            Subdomain = "test",
            StripeConnectAccountId = "acct_test",
            StripeConnectLastSyncedAt = DateTime.UtcNow.AddMinutes(-6) // 6 minutes ago
        };

        var shouldSync = tenant.StripeConnectLastSyncedAt == null ||
            DateTime.UtcNow - tenant.StripeConnectLastSyncedAt > TimeSpan.FromMinutes(5);

        Assert.True(shouldSync);
    }

    #endregion
}
