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
        /// Accepts a proposal amendment and transitions directly to FundingPlanReady,
        /// skipping the FinancialInfo and SiteVisit phases (which were already completed).
        /// </summary>
        Task<bool> AcceptProposalAmendmentAsync(Guid reserveStudyId, string acceptedBy);
        
        Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId);
        Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo);
        Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy);
        Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId);
        Task<bool> ResendProposalEmailAsync(Guid reserveStudyId);

        // UI helpers (native StudyStatus)
        Task<StudyStatus[]> GetAllowedStudyTransitionsAsync(Guid reserveStudyId);
        Task<bool> TryTransitionStudyAsync(Guid reserveStudyId, StudyStatus targetStatus, string? actor = null);
    }
}