using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CRS.Models.Workflow;
using CRS.Services.Interfaces;

namespace CRS.Services.Workflow {
    /// <summary>
    /// Pure state-machine service to manage StudyRequest transitions.
    /// Implements the 32-stage workflow.
    /// </summary>
    public class StudyWorkflowService : IStudyWorkflowService {
        private readonly INotificationService _notification;

        // Allowed transitions graph matching the 33-stage workflow
        private static readonly Dictionary<StudyStatus, HashSet<StudyStatus>> _allowed = new() {
            // ═══════════════════════════════════════════════════════════════
            // REQUEST PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.RequestCreated] = new() { 
                StudyStatus.RequestApproved,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.RequestApproved] = new() { 
                StudyStatus.ProposalCreated,
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // PROPOSAL PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.ProposalCreated] = new() { 
                StudyStatus.ProposalReviewed,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ProposalReviewed] = new() { 
                StudyStatus.ProposalUpdated, 
                StudyStatus.ProposalApproved,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ProposalUpdated] = new() { 
                StudyStatus.ProposalReviewed, 
                StudyStatus.ProposalApproved,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ProposalApproved] = new() { 
                StudyStatus.ProposalSent,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ProposalSent] = new() { 
                StudyStatus.ProposalAccepted, 
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ProposalAccepted] = new() { 
                StudyStatus.ServiceContactsRequested,
                StudyStatus.FinancialInfoRequested,
                StudyStatus.FundingPlanReady, // Amendment acceptance - skip to funding plan when amendment is accepted
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // DATA COLLECTION PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.ServiceContactsRequested] = new() { 
                StudyStatus.FinancialInfoRequested,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FinancialInfoRequested] = new() { 
                StudyStatus.FinancialInfoInProgress,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FinancialInfoInProgress] = new() { 
                StudyStatus.FinancialInfoSubmitted,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FinancialInfoSubmitted] = new() { 
                StudyStatus.FinancialInfoReviewed,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FinancialInfoReviewed] = new() { 
                StudyStatus.FinancialInfoReceived,
                StudyStatus.FinancialInfoRequested, // Request more info
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FinancialInfoReceived] = new() { 
                StudyStatus.SiteVisitPending,
                StudyStatus.FinancialInfoRequested, // Request more info if needed
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // SITE VISIT PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.SiteVisitPending] = new() { 
                StudyStatus.SiteVisitScheduled,
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.SiteVisitScheduled] = new() { 
                StudyStatus.SiteVisitCompleted,
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.SiteVisitCompleted] = new() { 
                StudyStatus.SiteVisitDataEntered,
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.SiteVisitDataEntered] = new() { 
                StudyStatus.FundingPlanReady,
                StudyStatus.AmendmentPending,       // If scope variance exceeds threshold
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // AMENDMENT PHASE (triggered by scope variance after site visit)
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.AmendmentPending] = new() {
                StudyStatus.AmendmentApproved,      // Admin approves amendment for sending to HOA
                StudyStatus.SiteVisitDataEntered,   // Go back if needs rework
                StudyStatus.RequestCancelled        // Cancelled before approval
            },

            [StudyStatus.AmendmentApproved] = new() {
                StudyStatus.FundingPlanReady,       // HOA accepted amendment
                StudyStatus.AmendmentPending,       // HOA rejected - needs revision
                StudyStatus.RequestCancelled        // HOA rejected and cancelled
            },

            // ═══════════════════════════════════════════════════════════════
            // FUNDING PLAN PHASE - Allow going back for more data
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.FundingPlanReady] = new() { 
                StudyStatus.FundingPlanInProcess,
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.SiteVisitDataEntered,   // Go back to enter more site data
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FundingPlanInProcess] = new() { 
                StudyStatus.FundingPlanComplete,
                StudyStatus.FundingPlanReady,       // Go back to ready state
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.FundingPlanComplete] = new() { 
                StudyStatus.NarrativeReady,
                StudyStatus.FundingPlanInProcess,   // Go back to recalculate
                StudyStatus.FinancialInfoRequested, // Request more financial info
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE/REPORT PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.NarrativeReady] = new() { 
                StudyStatus.NarrativeInProcess,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.NarrativeInProcess] = new() { 
                StudyStatus.NarrativeComplete,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.NarrativeComplete] = new() { 
                StudyStatus.NarrativePrintReady,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.NarrativePrintReady] = new() { 
                StudyStatus.NarrativePackaged,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.NarrativePackaged] = new() { 
                StudyStatus.NarrativeSent,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.NarrativeSent] = new() { 
                StudyStatus.ReportReady,
                StudyStatus.RequestCancelled 
            },

            // ═══════════════════════════════════════════════════════════════
            // FINAL REPORT PHASE
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.ReportReady] = new() { 
                StudyStatus.ReportInProcess,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ReportInProcess] = new() { 
                StudyStatus.ReportComplete,
                StudyStatus.RequestCancelled 
            },
            
            [StudyStatus.ReportComplete] = new() { 
                StudyStatus.FinalReviewPending,  // When RequireFinalReview enabled
                StudyStatus.RequestCompleted     // Direct completion when setting disabled
            },
            
            // Final review step (optional, based on tenant setting)
            [StudyStatus.FinalReviewPending] = new() { 
                StudyStatus.RequestCompleted 
            },

            // ═══════════════════════════════════════════════════════════════
            // COMPLETION
            // ═══════════════════════════════════════════════════════════════
            [StudyStatus.RequestCompleted] = new() { 
                StudyStatus.RequestArchived 
            },
            
            [StudyStatus.RequestCancelled] = new() { },  // Terminal state
            
            [StudyStatus.RequestArchived] = new() { }    // Terminal state
        };

        public StudyWorkflowService(INotificationService notification) {
            _notification = notification;
        }

        public bool IsTransitionAllowed(StudyStatus from, StudyStatus to) {
            return _allowed.TryGetValue(from, out var set) && set.Contains(to);
        }

        /// <summary>
        /// Checks if a transition is allowed for admin override (any transition except to same status).
        /// </summary>
        public bool IsAdminTransitionAllowed(StudyStatus from, StudyStatus to) {
            // Admins can transition to any status except the same one
            return from != to;
        }

        /// <summary>
        /// Gets all statuses that can be transitioned to from the current status.
        /// </summary>
        public StudyStatus[] GetAllowedTransitions(StudyStatus from) {
            return _allowed.TryGetValue(from, out var set) ? set.ToArray() : Array.Empty<StudyStatus>();
        }

        /// <summary>
        /// Gets all possible statuses for admin skip/rollback operations.
        /// </summary>
        public StudyStatus[] GetAllStatuses() {
            return Enum.GetValues<StudyStatus>();
        }

        public async Task<bool> TryTransitionAsync(StudyRequest request, StudyStatus newStatus, string? actor = null) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var old = request.CurrentStatus;
            if (!IsTransitionAllowed(old, newStatus)) return false;

            request.PreviousStatus = old;
            request.CurrentStatus = newStatus;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.StateChangedAt = request.UpdatedAt;
            request.StatusChangedBy = actor;

            await _notification.OnStateChangedAsync(request, old, newStatus);
            return true;
        }

        /// <summary>
        /// Forces a transition regardless of allowed transitions (admin only).
        /// </summary>
        public async Task<bool> ForceTransitionAsync(StudyRequest request, StudyStatus newStatus, string? actor = null) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var old = request.CurrentStatus;
            
            if (!IsAdminTransitionAllowed(old, newStatus)) return false;

            request.PreviousStatus = old;
            request.CurrentStatus = newStatus;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.StateChangedAt = request.UpdatedAt;
            request.StatusChangedBy = actor;

            await _notification.OnStateChangedAsync(request, old, newStatus);
            return true;
        }
    }
}
