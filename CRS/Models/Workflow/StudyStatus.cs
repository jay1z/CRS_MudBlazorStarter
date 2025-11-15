using System;

namespace CRS.Models.Workflow {
    /// <summary>
    /// Represents the lifecycle states for a Reserve Study request from submission to archival.
    /// </summary>
    public enum StudyStatus {
        /// <summary>
        /// Initial state when a board member creates a new request.
        /// Allowed: NewRequest -> PendingDetails
        /// </summary>
        NewRequest = 0,
        /// <summary>
        /// Waiting for all required details to be completed.
        /// Allowed: PendingDetails -> ReadyForReview
        /// </summary>
        PendingDetails = 1,
        /// <summary>
        /// All elements completed and ready for tenant review.
        /// Allowed: ReadyForReview -> Approved | NeedsInfo
        /// </summary>
        ReadyForReview = 2,
        /// <summary>
        /// Tenant requires more information from the HOA.
        /// Allowed: NeedsInfo -> ReadyForReview (once info provided)
        /// </summary>
        NeedsInfo = 3,
        /// <summary>
        /// Tenant approved the request and can assign a specialist.
        /// Allowed: Approved -> Assigned
        /// </summary>
        Approved = 4,
        /// <summary>
        /// A specialist has been assigned to the request.
        /// Allowed: Assigned -> ProposalPendingESign
        /// </summary>
        Assigned = 5,
        /// <summary>
        /// Proposal has been drafted and is pending HOA e-signature.
        /// Allowed: ProposalPendingESign -> Accepted | Rejected
        /// </summary>
        ProposalPendingESign = 6,
        /// <summary>
        /// HOA accepted the proposal (signed).
        /// Allowed: Accepted -> Scheduled
        /// </summary>
        Accepted = 7,
        /// <summary>
        /// HOA rejected the proposal (signed reject or expired).
        /// Terminal for this branch unless re-proposed by tenant.
        /// </summary>
        Rejected = 8,
        /// <summary>
        /// Inspection appointment has been scheduled.
        /// Allowed: Scheduled -> InProgress
        /// </summary>
        Scheduled = 9,
        /// <summary>
        /// Inspection is currently in progress.
        /// Allowed: InProgress -> UnderReview
        /// </summary>
        InProgress = 10,
        /// <summary>
        /// Data uploaded and under internal review.
        /// Allowed: UnderReview -> ReportDrafted | InProgress
        /// </summary>
        UnderReview = 11,
        /// <summary>
        /// Draft report created and awaiting QA.
        /// Allowed: ReportDrafted -> ApprovedReport
        /// </summary>
        ReportDrafted = 12,
        /// <summary>
        /// Final report approved after QA.
        /// Allowed: ApprovedReport -> Complete
        /// </summary>
        ApprovedReport = 13,
        /// <summary>
        /// Report delivered to HOA; workflow complete.
        /// Allowed: Complete -> Archived
        /// </summary>
        Complete = 14,
        /// <summary>
        /// Request archived. A3-year renewal reminder is scheduled.
        /// </summary>
        Archived = 15
    }
}
