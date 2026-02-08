using System;
using System.Threading.Tasks;

using CRS.Models;
using CRS.Models.Workflow;

namespace CRS.Services.Interfaces {
public interface IReserveStudyWorkflowService {
    Task<ReserveStudy> CreateReserveStudyRequestAsync(ReserveStudy reserveStudy);
    Task<bool> SendProposalAsync(Guid reserveStudyId, Proposal proposal);
    Task<bool> ApproveProposalAsync(Guid proposalId, string approvedBy);

    /// <summary>
    /// Reviews a proposal (required before approval when RequireProposalReview is enabled).
    /// </summary>
    Task<bool> ReviewProposalAsync(Guid proposalId, string reviewedBy);

    /// <summary>
    /// Accepts a proposal amendment and transitions directly to FundingPlanReady,
    /// skipping the FinancialInfo and SiteVisit phases (which were already completed).
    /// </summary>
    Task<bool> AcceptProposalAmendmentAsync(Guid reserveStudyId, string acceptedBy);

    /// <summary>
    /// Declines a proposal with reason and optional revision request.
    /// Transitions the study to ProposalDeclined status.
    /// </summary>
    /// <param name="proposalId">The proposal to decline</param>
    /// <param name="declinedBy">Name/identifier of who declined</param>
    /// <param name="reason">Category of decline reason</param>
    /// <param name="comments">Optional additional comments</param>
    /// <param name="requestRevision">Whether the customer wants a revised proposal</param>
    Task<bool> DeclineProposalAsync(Guid proposalId, string declinedBy, ProposalDeclineReason reason, string? comments, bool requestRevision);

    /// <summary>
    /// Creates a revised proposal after a decline, linking to the original.
    /// </summary>
    Task<Proposal?> CreateRevisionProposalAsync(Guid originalProposalId, string createdBy);

        Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId);
        Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo);
        Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy);
        Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId);

        /// <summary>
        /// Completes the final review and marks the study as complete.
        /// Used when RequireFinalReview tenant setting is enabled.
        /// </summary>
        Task<bool> CompleteFinalReviewAsync(Guid reserveStudyId, string reviewedBy);

        Task<bool> ResendProposalEmailAsync(Guid reserveStudyId);

        // UI helpers (native StudyStatus)
        Task<StudyStatus[]> GetAllowedStudyTransitionsAsync(Guid reserveStudyId);
        Task<bool> TryTransitionStudyAsync(Guid reserveStudyId, StudyStatus targetStatus, string? actor = null);
    }
}