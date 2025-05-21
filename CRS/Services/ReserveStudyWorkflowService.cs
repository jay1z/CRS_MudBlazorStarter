using System;
using System.Threading.Tasks;
using Coravel.Events.Interfaces;
using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services
{
    public class ReserveStudyWorkflowService : IReserveStudyWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDispatcher _dispatcher;
        private readonly IReserveStudyService _reserveStudyService;

        public ReserveStudyWorkflowService(
            ApplicationDbContext context,
            IDispatcher dispatcher,
            IReserveStudyService reserveStudyService)
        {
            _context = context;
            _dispatcher = dispatcher;
            _reserveStudyService = reserveStudyService;
        }

        public async Task<ReserveStudy> CreateReserveStudyRequestAsync(ReserveStudy reserveStudy)
        {
            // Save the reserve study
            var createdStudy = await _reserveStudyService.CreateReserveStudyAsync(reserveStudy);
            
            // Broadcast event to send notifications
            //await _dispatcher.Broadcast(new ReserveStudyCreatedEvent(createdStudy));
            
            return createdStudy;
        }

        public async Task<bool> SendProposalAsync(Guid reserveStudyId, Proposal proposal)
        {
            var study = await _context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            proposal.ReserveStudyId = reserveStudyId;
            proposal.DateSent = DateTime.UtcNow;
            
            _context.Add(proposal);
            study.Status = ReserveStudy.WorkflowStatus.ProposalSent;
            
            await _context.SaveChangesAsync();
            
            // Broadcast event to send proposal notification
            //await _dispatcher.Broadcast(new ProposalSentEvent(study, proposal));
            
            return true;
        }

        public async Task<bool> ApproveProposalAsync(Guid proposalId, string approvedBy)
        {
            var proposal = await _context.Set<Proposal>()
                .Include(p => p.ReserveStudy)
                .FirstOrDefaultAsync(p => p.Id == proposalId);
                
            if (proposal == null || proposal.ReserveStudy == null)
                return false;
                
            proposal.IsApproved = true;
            proposal.DateApproved = DateTime.UtcNow;
            proposal.ApprovedBy = approvedBy;
            
            proposal.ReserveStudy.Status = ReserveStudy.WorkflowStatus.ProposalApproved;
            
            await _context.SaveChangesAsync();
            
            // Broadcast event for proposal approved notification
            //await _dispatcher.Broadcast(new ProposalApprovedEvent(proposal.ReserveStudy, proposal));
            
            return true;
        }

        public async Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId)
        {
            var study = await _context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;
                
            study.Status = ReserveStudy.WorkflowStatus.FinancialInfoRequested;
            
            await _context.SaveChangesAsync();
            
            // Broadcast event for financial info request notification
            //await _dispatcher.Broadcast(new FinancialInfoRequestedEvent(study));
            
            return true;
        }

        public async Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo)
        {
            var study = await _context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            financialInfo.ReserveStudyId = reserveStudyId;
            financialInfo.DateSubmitted = DateTime.UtcNow;
            
            _context.Add(financialInfo);
            study.Status = ReserveStudy.WorkflowStatus.FinancialInfoSubmitted;
            
            await _context.SaveChangesAsync();
            
            // Broadcast event for financial info submitted notification
            //await _dispatcher.Broadcast(new FinancialInfoSubmittedEvent(study, financialInfo));
            
            return true;
        }

        public async Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy)
        {
            var financialInfo = await _context.Set<FinancialInfo>()
                .Include(f => f.ReserveStudy)
                .FirstOrDefaultAsync(f => f.Id == financialInfoId);
                
            if (financialInfo == null || financialInfo.ReserveStudy == null)
                return false;
                
            financialInfo.IsComplete = true;
            financialInfo.DateReviewed = DateTime.UtcNow;
            financialInfo.ReviewedBy = reviewedBy;
            
            financialInfo.ReserveStudy.Status = ReserveStudy.WorkflowStatus.FinancialInfoReviewed;
            
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId)
        {
            var study = await _context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;
                
            study.Status = ReserveStudy.WorkflowStatus.RequestCompleted;
            study.IsComplete = true;
            
            await _context.SaveChangesAsync();
            
            // Broadcast completion event
            //await _dispatcher.Broadcast(new ReserveStudyCompletedEvent(study));
            
            return true;
        }
    }
}