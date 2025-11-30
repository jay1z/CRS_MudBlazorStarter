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
        public string? PendingOwnerEmail { get; set; } // captured during signup; used to create owner after payment
        public Guid SignupToken { get; set; } = Guid.NewGuid(); // used to finalize owner setup securely
        public string? LastStripeCheckoutSessionId { get; set; } // set when created via post-payment flow
        
        // Subscription lifecycle management
        public DateTime? SuspendedAt { get; set; } // When account was suspended (no access)
        public DateTime? GracePeriodEndsAt { get; set; } // When grace period expires (read-only access ends)
        public DateTime? DeletionScheduledAt { get; set; } // When data will be permanently deleted
        public bool IsMarkedForDeletion { get; set; } = false; // Soft delete flag
        public string? DeletionReason { get; set; } // Reason: "payment_failed", "user_requested", etc.
        
        // Reactivation tracking
        public int ReactivationCount { get; set; } = 0; // How many times account was reactivated
        public DateTime? LastReactivatedAt { get; set; } // Last time subscription was restored
        public DateTime? LastPaymentFailureAt { get; set; } // Track when payment last failed
        
        // Demo Mode: Mark demo tenants and track soft delete
        public bool IsDemo { get; set; } = false;
        public DateTime? DateDeleted { get; set; }
        
        // Owner reference for demo cleanup
        public string? OwnerId { get; set; }
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
        PastDue = 3, // Payment failed, Stripe retrying (Days 0-7)
        Canceled = 4,
        Unpaid = 5,
        Trialing = 6,
        GracePeriod = 7, // Read-only access, data preserved (Days 8-30)
        Suspended = 8, // No access, data preserved (Days 31-90)
        MarkedForDeletion = 9 // Soft delete, pending permanent deletion (Days 91-365)
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
