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
        
        // Workflow Settings: Auto-accept new reserve study requests
        /// <summary>
        /// When enabled, new reserve study requests from HOA users are automatically approved
        /// without requiring manual review by tenant staff.
        /// </summary>
        public bool AutoAcceptStudyRequests { get; set; } = false;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Proposal Phase
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Default number of days before a proposal expires. Set to 0 for no expiration.
        /// </summary>
        public int DefaultProposalExpirationDays { get; set; } = 30;
        
        /// <summary>
        /// When enabled, proposals are automatically sent to clients when internally approved.
        /// </summary>
        public bool AutoSendProposalOnApproval { get; set; } = false;
        
        /// <summary>
        /// When enabled, requires a second reviewer before proposal can be sent.
        /// </summary>
        public bool RequireProposalReview { get; set; } = false;
        
        /// <summary>
        /// When enabled, skip the proposal step for trusted/repeat clients and proceed directly to engagement.
        /// </summary>
        public bool SkipProposalStep { get; set; } = false;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Data Collection Phase
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// When enabled, automatically request financial info after proposal acceptance.
        /// </summary>
        public bool AutoRequestFinancialInfo { get; set; } = true;
        
        /// <summary>
        /// Default deadline in days for financial info submission.
        /// </summary>
        public int FinancialInfoDueDays { get; set; } = 14;
        
        /// <summary>
        /// When enabled, requires service contact collection during data gathering.
        /// </summary>
        public bool RequireServiceContacts { get; set; } = false;
        
        /// <summary>
        /// When enabled, skip financial data collection for certain study types.
        /// </summary>
        public bool SkipFinancialInfoStep { get; set; } = false;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Site Visit Phase
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// When enabled, site visits are mandatory before completing a study.
        /// </summary>
        public bool RequireSiteVisit { get; set; } = true;
        
        /// <summary>
        /// Default duration in minutes for site visit calendar blocks.
        /// </summary>
        public int DefaultSiteVisitDurationMinutes { get; set; } = 120;
        
        /// <summary>
        /// When enabled, allows video/virtual site inspections instead of in-person visits.
        /// </summary>
        public bool AllowVirtualSiteVisit { get; set; } = false;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Notifications & Reminders
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// When enabled, automatic reminder emails are sent for pending actions.
        /// </summary>
        public bool SendAutomaticReminders { get; set; } = true;
        
        /// <summary>
        /// How often to send reminder emails (in days).
        /// </summary>
        public int ReminderFrequencyDays { get; set; } = 7;
        
        /// <summary>
        /// When enabled, tenant owner is notified on major status changes.
        /// </summary>
        public bool NotifyOwnerOnStatusChange { get; set; } = true;
        
        /// <summary>
        /// When enabled, HOA client is notified via email on major status changes.
        /// </summary>
        public bool NotifyClientOnStatusChange { get; set; } = true;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Invoice Integration
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// When enabled, a draft invoice is automatically created when a proposal is accepted.
        /// </summary>
        public bool AutoGenerateInvoiceOnAcceptance { get; set; } = false;
        
        /// <summary>
        /// Default payment terms in days (e.g., 30 = Net 30).
        /// </summary>
        public int DefaultPaymentTermsDays { get; set; } = 30;
        
        /// <summary>
        /// When enabled, automatic payment reminder emails are sent for overdue invoices.
        /// </summary>
        public bool AutoSendInvoiceReminders { get; set; } = true;
        
        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW SETTINGS - Report & Completion
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// When enabled, requires owner review of narrative before it can be used in reports.
        /// When disabled, specialists can mark narratives as approved directly.
        /// </summary>
        public bool RequireNarrativeReview { get; set; } = false;

        /// <summary>
        /// When enabled, requires owner review before marking a study complete.
        /// </summary>
        public bool RequireFinalReview { get; set; } = false;
        
        /// <summary>
        /// Number of days after completion before auto-archiving. Set to 0 to disable.
        /// </summary>
        public int AutoArchiveAfterDays { get; set; } = 0;
        
        /// <summary>
        /// When enabled, amendments are allowed after a study is completed.
        /// </summary>
        public bool AllowAmendmentsAfterCompletion { get; set; } = true;
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
