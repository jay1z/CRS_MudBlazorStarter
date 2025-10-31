using System;
using System.Threading.Tasks;

using Coravel.Events.Interfaces;

using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRS.Services {
    public class ReserveStudyWorkflowService : IReserveStudyWorkflowService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IDispatcher _dispatcher;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReserveStudyWorkflowService(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            IDispatcher dispatcher,
            IReserveStudyService reserveStudyService,
            ITenantContext tenantContext,
            IHttpContextAccessor httpContextAccessor) {
            _dbFactory = dbFactory;
            _dispatcher = dispatcher;
            _reserveStudyService = reserveStudyService;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsCurrentUserInRole(params string[] roles) {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;
            foreach (var role in roles) {
                if (user.IsInRole(role)) return true;
            }
            return false;
        }

        public async Task<ReserveStudy> CreateReserveStudyRequestAsync(ReserveStudy reserveStudy) {
            // Save the reserve study
            var createdStudy = await _reserveStudyService.CreateReserveStudyAsync(reserveStudy);

            // Broadcast event to send notifications
            //await _dispatcher.Broadcast(new ReserveStudyCreatedEvent(createdStudy));

            return createdStudy;
        }

        public async Task<bool> SendProposalAsync(Guid reserveStudyId, Proposal proposal) {
            // Only Specialist or Admin may send proposals
            if (!IsCurrentUserInRole("Specialist", "Admin")) return false;

            await using var context = await _dbFactory.CreateDbContextAsync();

            var study = await context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            // Ensure proposal is tenant-scoped
            var tenantId = _tenantContext.TenantId ??1;
            if (proposal.TenantId ==0) proposal.TenantId = tenantId;

            proposal.ReserveStudyId = reserveStudyId;
            proposal.DateSent = DateTime.UtcNow;

            context.Add(proposal);
            study.Status = ReserveStudy.WorkflowStatus.ProposalSent;

            await context.SaveChangesAsync();

            // Broadcast event to send proposal notification
            //await _dispatcher.Broadcast(new ProposalSentEvent(study, proposal));

            return true;
        }

        public async Task<bool> ApproveProposalAsync(Guid proposalId, string approvedBy) {
            // Only Specialist or Admin may approve proposals
            if (!IsCurrentUserInRole("Specialist", "Admin")) return false;

            await using var context = await _dbFactory.CreateDbContextAsync();

            var proposal = await context.Set<Proposal>()
                .Include(p => p.ReserveStudy)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null || proposal.ReserveStudy == null)
                return false;

            proposal.IsApproved = true;
            proposal.DateApproved = DateTime.UtcNow;
            proposal.ApprovedBy = approvedBy;

            proposal.ReserveStudy.Status = ReserveStudy.WorkflowStatus.ProposalApproved;

            await context.SaveChangesAsync();

            // Broadcast event for proposal approved notification
            //await _dispatcher.Broadcast(new ProposalApprovedEvent(proposal.ReserveStudy, proposal));

            return true;
        }

        public async Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId) {
            // Only Specialist or Admin may request financial info
            if (!IsCurrentUserInRole("Specialist", "Admin")) return false;

            await using var context = await _dbFactory.CreateDbContextAsync();

            var study = await context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            study.Status = ReserveStudy.WorkflowStatus.FinancialInfoRequested;

            await context.SaveChangesAsync();

            // Broadcast event for financial info request notification
            //await _dispatcher.Broadcast(new FinancialInfoRequestedEvent(study));

            return true;
        }

        public async Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo) {
            // Allow submission by any authenticated user (client) — tenant scoping is enforced
            await using var context = await _dbFactory.CreateDbContextAsync();

            var study = await context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            // Ensure tenant id is set on financial info
            var tenantId = _tenantContext.TenantId ??1;
            if (financialInfo.TenantId ==0) financialInfo.TenantId = tenantId;

            financialInfo.ReserveStudyId = reserveStudyId;
            financialInfo.DateSubmitted = DateTime.UtcNow;

            context.Add(financialInfo);
            study.Status = ReserveStudy.WorkflowStatus.FinancialInfoSubmitted;

            await context.SaveChangesAsync();

            // Broadcast event for financial info submitted notification
            //await _dispatcher.Broadcast(new FinancialInfoSubmittedEvent(study, financialInfo));

            return true;
        }

        public async Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy) {
            // Only Specialist or Admin may review financial info
            if (!IsCurrentUserInRole("Specialist", "Admin")) return false;

            await using var context = await _dbFactory.CreateDbContextAsync();

            var financialInfo = await context.Set<FinancialInfo>()
                .Include(f => f.ReserveStudy)
                .FirstOrDefaultAsync(f => f.Id == financialInfoId);

            if (financialInfo == null || financialInfo.ReserveStudy == null)
                return false;

            financialInfo.IsComplete = true;
            financialInfo.DateReviewed = DateTime.UtcNow;
            financialInfo.ReviewedBy = reviewedBy;

            financialInfo.ReserveStudy.Status = ReserveStudy.WorkflowStatus.FinancialInfoReviewed;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId) {
            // Only Specialist or Admin may mark complete
            if (!IsCurrentUserInRole("Specialist", "Admin")) return false;

            await using var context = await _dbFactory.CreateDbContextAsync();

            var study = await context.ReserveStudies.FindAsync(reserveStudyId);
            if (study == null)
                return false;

            study.Status = ReserveStudy.WorkflowStatus.RequestCompleted;
            study.IsComplete = true;

            await context.SaveChangesAsync();

            // Broadcast completion event
            //await _dispatcher.Broadcast(new ReserveStudyCompletedEvent(study));

            return true;
        }
    }
}