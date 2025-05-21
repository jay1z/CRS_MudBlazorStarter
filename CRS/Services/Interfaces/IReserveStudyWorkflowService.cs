using System;
using System.Threading.Tasks;

using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface IReserveStudyWorkflowService {
        Task<ReserveStudy> CreateReserveStudyRequestAsync(ReserveStudy reserveStudy);
        Task<bool> SendProposalAsync(Guid reserveStudyId, Proposal proposal);
        Task<bool> ApproveProposalAsync(Guid proposalId, string approvedBy);
        Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId);
        Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo);
        Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy);
        Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId);
    }
}