using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace CRS.Services.Billing;

/// <summary>
/// Service for managing Stripe Connect accounts for tenants.
/// Allows tenants to receive payments from their HOA clients.
/// 
/// Flow:
/// 1. Tenant clicks "Connect Stripe Account" in their dashboard
/// 2. We create a V2 Connect account and redirect to Stripe's onboarding
/// 3. After onboarding, tenant can receive payments via Direct Charges
/// 4. Platform fee (application_fee) is automatically collected by us
/// </summary>
public interface IStripeConnectService
{
    /// <summary>
    /// Creates a new Stripe Connect account for a tenant and returns the onboarding URL.
    /// </summary>
    Task<StripeConnectOnboardingResult> CreateAccountAndGetOnboardingUrlAsync(
        int tenantId, 
        string returnUrl, 
        string refreshUrl, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets an onboarding URL for an existing Connect account that needs to complete onboarding.
    /// </summary>
    Task<string?> GetOnboardingUrlAsync(
        int tenantId, 
        string returnUrl, 
        string refreshUrl, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Syncs the Connect account status from Stripe (onboarding complete, payouts enabled, etc.)
    /// </summary>
    Task<StripeConnectStatus> SyncAccountStatusAsync(int tenantId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the current Connect account status for a tenant.
    /// </summary>
    Task<StripeConnectStatus> GetAccountStatusAsync(int tenantId, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a Stripe Express Dashboard login link for the tenant.
    /// </summary>
    Task<string?> CreateDashboardLoginLinkAsync(int tenantId, CancellationToken ct = default);
    
    /// <summary>
    /// Calculates the application fee amount for a given payment amount based on tenant's tier.
    /// </summary>
    Task<long> CalculateApplicationFeeAsync(int tenantId, decimal paymentAmount, CancellationToken ct = default);
}

/// <summary>
/// Result of creating a Connect account and starting onboarding.
/// </summary>
public record StripeConnectOnboardingResult(
    bool Success,
    string? AccountId,
    string? OnboardingUrl,
    string? ErrorMessage
);

/// <summary>
/// Current status of a tenant's Stripe Connect account.
/// </summary>
public record StripeConnectStatus(
    bool HasConnectAccount,
    string? AccountId,
    bool OnboardingComplete,
    bool PayoutsEnabled,
    bool CardPaymentsEnabled,
    bool RequiresAction,
    string? RequirementsStatus,
    DateTime? LastSynced
);

public class StripeConnectService : IStripeConnectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IStripeClientFactory _stripeClientFactory;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<StripeConnectService> _logger;

    public StripeConnectService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IStripeClientFactory stripeClientFactory,
        IOptions<StripeOptions> stripeOptions,
        ILogger<StripeConnectService> logger)
    {
        _dbFactory = dbFactory;
        _stripeClientFactory = stripeClientFactory;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    public async Task<StripeConnectOnboardingResult> CreateAccountAndGetOnboardingUrlAsync(
        int tenantId,
        string returnUrl,
        string refreshUrl,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        
        if (tenant == null)
        {
            return new StripeConnectOnboardingResult(false, null, null, "Tenant not found");
        }

        // If tenant already has a Connect account, just get onboarding URL
        if (!string.IsNullOrEmpty(tenant.StripeConnectAccountId))
        {
            var url = await GetOnboardingUrlAsync(tenantId, returnUrl, refreshUrl, ct);
            return new StripeConnectOnboardingResult(true, tenant.StripeConnectAccountId, url, null);
        }

        try
        {
            var client = _stripeClientFactory.CreateClient();
            
            // Get tenant owner email for the Connect account
            string? ownerEmail = tenant.PendingOwnerEmail;
            if (string.IsNullOrEmpty(ownerEmail) && !string.IsNullOrEmpty(tenant.OwnerId))
            {
                var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(tenant.OwnerId), ct);
                ownerEmail = owner?.Email;
            }

            // Create V2 Connect Account using the new API
            // Note: Using V1 API for now as V2 may not be fully available in SDK
            // When V2 is available, switch to: stripeClient.V2.Core.Accounts.CreateAsync
            var accountService = new AccountService(client);
            var accountOptions = new AccountCreateOptions
            {
                Type = "express", // Express accounts are easiest for onboarding
                Country = "US",
                Email = ownerEmail,
                BusinessType = "company",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    UsBankAccountAchPayments = new AccountCapabilitiesUsBankAccountAchPaymentsOptions { Requested = true }
                },
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    Name = tenant.Name,
                    ProductDescription = "Reserve study services for HOA communities"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenant.Id.ToString(),
                    ["tenant_subdomain"] = tenant.Subdomain
                }
            };

            var account = await accountService.CreateAsync(accountOptions, cancellationToken: ct);

            // Save the Connect account ID
            tenant.StripeConnectAccountId = account.Id;
            tenant.StripeConnectCreatedAt = DateTime.UtcNow;
            tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created Stripe Connect account {AccountId} for tenant {TenantId} ({TenantName})",
                account.Id, tenantId, tenant.Name);

            // Create onboarding link
            var onboardingUrl = await CreateAccountLinkAsync(account.Id, returnUrl, refreshUrl, ct);

            return new StripeConnectOnboardingResult(true, account.Id, onboardingUrl, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe Connect account for tenant {TenantId}", tenantId);
            return new StripeConnectOnboardingResult(false, null, null, ex.Message);
        }
    }

    public async Task<string?> GetOnboardingUrlAsync(
        int tenantId,
        string returnUrl,
        string refreshUrl,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null || string.IsNullOrEmpty(tenant.StripeConnectAccountId))
        {
            return null;
        }

        return await CreateAccountLinkAsync(tenant.StripeConnectAccountId, returnUrl, refreshUrl, ct);
    }

    private async Task<string?> CreateAccountLinkAsync(
        string accountId,
        string returnUrl,
        string refreshUrl,
        CancellationToken ct)
    {
        try
        {
            var client = _stripeClientFactory.CreateClient();
            var accountLinkService = new AccountLinkService(client);

            var linkOptions = new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding"
            };

            var link = await accountLinkService.CreateAsync(linkOptions, cancellationToken: ct);
            return link.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create account link for {AccountId}", accountId);
            return null;
        }
    }

    public async Task<StripeConnectStatus> SyncAccountStatusAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null)
        {
            return new StripeConnectStatus(false, null, false, false, false, false, null, null);
        }

        if (string.IsNullOrEmpty(tenant.StripeConnectAccountId))
        {
            return new StripeConnectStatus(false, null, false, false, false, false, null, null);
        }

        try
        {
            var client = _stripeClientFactory.CreateClient();
            var accountService = new AccountService(client);
            var account = await accountService.GetAsync(tenant.StripeConnectAccountId, cancellationToken: ct);

            // Update tenant with current status
            tenant.StripeConnectPayoutsEnabled = account.PayoutsEnabled;
            tenant.StripeConnectCardPaymentsEnabled = 
                account.Capabilities?.CardPayments == "active";
            
            // Check if onboarding is complete (no currently_due requirements)
            var hasCurrentlyDue = account.Requirements?.CurrentlyDue?.Any() ?? false;
            var hasPastDue = account.Requirements?.PastDue?.Any() ?? false;
            tenant.StripeConnectOnboardingComplete = !hasCurrentlyDue && !hasPastDue && account.DetailsSubmitted == true;
            
            tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            var requirementsStatus = hasCurrentlyDue ? "currently_due" : hasPastDue ? "past_due" : "complete";

            _logger.LogInformation(
                "Synced Stripe Connect status for tenant {TenantId}: Onboarding={Onboarding}, Payouts={Payouts}, Cards={Cards}",
                tenantId, tenant.StripeConnectOnboardingComplete, tenant.StripeConnectPayoutsEnabled, 
                tenant.StripeConnectCardPaymentsEnabled);

            return new StripeConnectStatus(
                HasConnectAccount: true,
                AccountId: tenant.StripeConnectAccountId,
                OnboardingComplete: tenant.StripeConnectOnboardingComplete,
                PayoutsEnabled: tenant.StripeConnectPayoutsEnabled,
                CardPaymentsEnabled: tenant.StripeConnectCardPaymentsEnabled,
                RequiresAction: hasCurrentlyDue || hasPastDue,
                RequirementsStatus: requirementsStatus,
                LastSynced: tenant.StripeConnectLastSyncedAt
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to sync Stripe Connect status for tenant {TenantId}", tenantId);
            return new StripeConnectStatus(
                true, tenant.StripeConnectAccountId, 
                tenant.StripeConnectOnboardingComplete, 
                tenant.StripeConnectPayoutsEnabled,
                tenant.StripeConnectCardPaymentsEnabled,
                false, "error", tenant.StripeConnectLastSyncedAt);
        }
    }

    public async Task<StripeConnectStatus> GetAccountStatusAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null || string.IsNullOrEmpty(tenant.StripeConnectAccountId))
        {
            return new StripeConnectStatus(false, null, false, false, false, false, null, null);
        }

        // If we haven't synced recently (within 5 minutes), sync now
        if (tenant.StripeConnectLastSyncedAt == null || 
            DateTime.UtcNow - tenant.StripeConnectLastSyncedAt > TimeSpan.FromMinutes(5))
        {
            return await SyncAccountStatusAsync(tenantId, ct);
        }

        return new StripeConnectStatus(
            HasConnectAccount: true,
            AccountId: tenant.StripeConnectAccountId,
            OnboardingComplete: tenant.StripeConnectOnboardingComplete,
            PayoutsEnabled: tenant.StripeConnectPayoutsEnabled,
            CardPaymentsEnabled: tenant.StripeConnectCardPaymentsEnabled,
            RequiresAction: !tenant.StripeConnectOnboardingComplete,
            RequirementsStatus: tenant.StripeConnectOnboardingComplete ? "complete" : "pending",
            LastSynced: tenant.StripeConnectLastSyncedAt
        );
    }

    public async Task<string?> CreateDashboardLoginLinkAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null || string.IsNullOrEmpty(tenant.StripeConnectAccountId))
        {
            return null;
        }

        try
        {
            var client = _stripeClientFactory.CreateClient();
            // Create a login link for Express/Standard accounts to access their Stripe dashboard
            var loginLinkService = new Stripe.AccountLoginLinkService(client);
            var loginLink = await loginLinkService.CreateAsync(
                tenant.StripeConnectAccountId, 
                cancellationToken: ct);
            return loginLink.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create dashboard login link for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<long> CalculateApplicationFeeAsync(int tenantId, decimal paymentAmount, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null)
        {
            // Default to Pro rate if tenant not found
            var defaultRate = SubscriptionTierDefaults.ProPlatformFeeRate;
            return (long)(paymentAmount * defaultRate * 100); // Convert to cents
        }

        // Get the platform fee rate based on tenant's tier (or override)
        var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);
        
        // Calculate fee in cents (Stripe uses integer cents)
        var feeInCents = (long)Math.Ceiling(paymentAmount * feeRate * 100);
        
        _logger.LogDebug(
            "Calculated application fee for tenant {TenantId} (Tier: {Tier}): {Amount:C} × {Rate:P2} = {Fee} cents",
            tenantId, tenant.Tier, paymentAmount, feeRate, feeInCents);

        return feeInCents;
    }
}
