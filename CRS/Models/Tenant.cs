using System;
using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    // SaaS Refactor: Tenant entity for multi-tenant support + Billing
    public class Tenant {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Subdomain { get; set; }
        public bool IsActive { get; set; } = true;
        public string? BrandingJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // New: time-ordered public identifier (UUIDv7) - use runtime-provided GUID v7
        public Guid PublicId { get; set; } = Guid.CreateVersion7();

        // Provisioning lifecycle
        public TenantProvisioningStatus ProvisioningStatus { get; set; } = TenantProvisioningStatus.Pending; // initial
        public DateTime? ProvisionedAt { get; set; }
        public string? ProvisioningError { get; set; }

        // Billing: Subscription tier (null = not subscribed yet)
        public SubscriptionTier? Tier { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public int MaxCommunities { get; set; } = 0; // set when activated
        public int MaxSpecialistUsers { get; set; } = 0; // set when activated
        public DateTime? UpdatedAt { get; set; }
        // Optional: track subscription status separately from IsActive (e.g., PastDue, Canceled)
        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.None;
        public DateTime? SubscriptionActivatedAt { get; set; }
        public DateTime? SubscriptionCanceledAt { get; set; }
    }

    public enum TenantProvisioningStatus {
        Pending = 0,
        Provisioning = 1,
        Active = 2,
        Failed = 3,
        Disabled = 4
    }

    // Billing tiers
    public enum SubscriptionTier {
        Startup = 0,
        Pro = 1,
        Enterprise = 2
    }

    // Subscription lifecycle status (Stripe derived)
    public enum SubscriptionStatus {
        None = 0, // no subscription yet
        Incomplete = 1,
        Active = 2,
        PastDue = 3,
        Canceled = 4,
        Unpaid = 5,
        Trialing = 6
    }

    public static class SubscriptionTierDefaults {
        public const int StartupMaxCommunities = 10;
        public const int StartupMaxSpecialistUsers = 2;
        public const int ProMaxCommunities = 50;
        public const int ProMaxSpecialistUsers = 10;
        // Enterprise is configurable per-tenant; set baseline minimums
        public const int EnterpriseMinCommunities = 100;
        public const int EnterpriseMinSpecialistUsers = 20;

        public static (int communities, int specialists) GetLimits(SubscriptionTier tier) => tier switch {
            SubscriptionTier.Startup => (StartupMaxCommunities, StartupMaxSpecialistUsers),
            SubscriptionTier.Pro => (ProMaxCommunities, ProMaxSpecialistUsers),
            SubscriptionTier.Enterprise => (EnterpriseMinCommunities, EnterpriseMinSpecialistUsers),
            _ => (0, 0)
        };
    }
}
