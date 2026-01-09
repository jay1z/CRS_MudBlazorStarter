using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using CRS.Services.Tenant;

namespace CRS.Models.Workflow {
    /// <summary>
    /// Represents a single study request tracked by the workflow engine.
    /// Shared primary key (Id) maps1:1 to ReserveStudy.Id.
    /// </summary>
    [Index(nameof(TenantId), nameof(CurrentStatus))]
    [Index(nameof(TenantId), nameof(StateChangedAt))]
    [Index(nameof(CommunityId))]
    public class StudyRequest : ITenantScoped {
        /// <summary>Unique id (also FK to ReserveStudy.Id).</summary>
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        /// <summary>Tenant that owns this request.</summary>
        public int TenantId { get; set; }

        /// <summary>Community id (external reference).</summary>
        public Guid CommunityId { get; set; }

        /// <summary>Current status in the workflow.</summary>
        public StudyStatus CurrentStatus { get; set; } = StudyStatus.RequestCreated;

        // ─────────────────────────────────────────────────────────────
        // HOA Element Count Estimates (captured during request creation)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Estimated number of building elements (e.g., roofs, siding).</summary>
        public int? EstimatedBuildingElementCount { get; set; }

        /// <summary>Estimated number of common elements (e.g., pools, clubhouses).</summary>
        public int? EstimatedCommonElementCount { get; set; }

        /// <summary>Estimated number of additional/custom elements.</summary>
        public int? EstimatedAdditionalElementCount { get; set; }

        /// <summary>Total estimated element count.</summary>
        [NotMapped]
        public int EstimatedTotalCount => 
            (EstimatedBuildingElementCount ?? 0) + 
            (EstimatedCommonElementCount ?? 0) + 
            (EstimatedAdditionalElementCount ?? 0);

        /// <summary>Optional notes about property elements from HOA.</summary>
        [MaxLength(2000)]
        public string? ElementEstimateNotes { get; set; }

        // ─────────────────────────────────────────────────────────────
        // Timestamps and Status
        // ─────────────────────────────────────────────────────────────

        /// <summary>When the request was created.</summary>
        [DataType(DataType.DateTime)]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>When the request was last updated (any changes).</summary>
        [DataType(DataType.DateTime)]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>When the status last changed.</summary>
        [DataType(DataType.DateTime)]
        public DateTimeOffset StateChangedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Optional actor who changed the status.</summary>
        [MaxLength(256)]
        public string? StatusChangedBy { get; set; }

        /// <summary>Optional notes attached to the status change.</summary>
        [MaxLength(1024)]
        public string? StatusNotes { get; set; }

        /// <summary>Previous status (stored for cancellation reversal).</summary>
        public StudyStatus? PreviousStatus { get; set; }

        // ─────────────────────────────────────────────────────────────
        // Stage Completion Flags (Option 4: Boolean tracking)
        // These track what has been completed, independent of current stage
        // ─────────────────────────────────────────────────────────────

        /// <summary>Whether the initial proposal has been accepted by HOA.</summary>
        public bool ProposalAccepted { get; set; }

        /// <summary>When the initial proposal was accepted.</summary>
        public DateTime? ProposalAcceptedAt { get; set; }

        /// <summary>Whether an amendment is required due to scope variance.</summary>
        public bool AmendmentRequired { get; set; }

        /// <summary>Whether the amendment has been accepted by HOA.</summary>
        public bool AmendmentAccepted { get; set; }

        /// <summary>When the amendment was accepted.</summary>
        public DateTime? AmendmentAcceptedAt { get; set; }

        /// <summary>Whether the site visit has been completed.</summary>
        public bool SiteVisitComplete { get; set; }

        /// <summary>When the site visit was completed.</summary>
        public DateTime? SiteVisitCompletedAt { get; set; }

        /// <summary>Concurrency token.</summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        /// <summary>1:1 navigation to the associated ReserveStudy (shared PK on Id).</summary>
        public CRS.Models.ReserveStudy? ReserveStudy { get; set; }
    }
}
