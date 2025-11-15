using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using CRS.Services.Workflow;
using Coravel.Events.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    /// <summary>
    /// Orchestrates legacy ReserveStudy workflow operations while enforcing centralized transitions
    /// via the StudyWorkflowService (state machine) backed by the StudyRequest record.
    /// </summary>
    public partial class ReserveStudyWorkflowService : IReserveStudyWorkflowService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStudyWorkflowService _engine;
        private readonly INotificationService _notifier;
        private readonly IDispatcher _dispatcher;

        public ReserveStudyWorkflowService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IReserveStudyService reserveStudyService,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IStudyWorkflowService engine,
        INotificationService notifier,
        IDispatcher dispatcher) {
            _dbFactory = dbFactory;
            _reserveStudyService = reserveStudyService;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
            _engine = engine;
            _notifier = notifier;
            _dispatcher = dispatcher;
        }

        private bool IsCurrentUserInRole(params string[] roles) {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return false;
            foreach (var role in roles) {
                if (user.IsInRole(role)) return true;
            }
            return false;
        }

        public async Task<ReserveStudy> CreateReserveStudyRequestAsync(ReserveStudy reserveStudy) {
            // Persist ReserveStudy through domain service (keeps behavior consistent across app)
            var created = await _reserveStudyService.CreateReserveStudyAsync(reserveStudy);

            await using var db = await _dbFactory.CreateDbContextAsync();
            // Ensure StudyRequest exists1:1 (keyed by ReserveStudy.Id)
            var sr = await db.StudyRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == created.Id);
            if (sr == null) {
                sr = ToStudyRequest(created);
                await db.StudyRequests.AddAsync(sr);
                // Initial history entry (creation)
                await AppendHistoryAsync(db, sr, sr.CurrentStatus, sr.CurrentStatus, GetActor());
                await db.SaveChangesAsync();
            }

            return created;
        }

        public async Task<bool> SendProposalAsync(Guid reserveStudyId, Proposal proposal) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var study = await db.ReserveStudies
            .Include(s => s.Proposal)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;

            // Upsert proposal
            proposal.ReserveStudyId = study.Id;
            proposal.TenantId = study.TenantId;
            if (study.Proposal == null) {
                await db.Proposals.AddAsync(proposal);
                study.Proposal = proposal;
            } else {
                // Update existing
                study.Proposal.ProposalScope = proposal.ProposalScope;
                study.Proposal.EstimatedCost = proposal.EstimatedCost;
                study.Proposal.Comments = proposal.Comments;
                study.Proposal.DateSent = DateTime.UtcNow;
            }

            // Enforce transition -> ProposalSent => ProposalPendingESign
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.ProposalPendingESign; // mapped from legacy ProposalSent
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            // Map back to legacy and persist
            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;

            // History row
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            await db.SaveChangesAsync();

            // Fire event for email workflows/listeners
            await _dispatcher.Broadcast(new ProposalSentEvent(study, study.Proposal ?? proposal));
            return true;
        }

        public async Task<bool> ApproveProposalAsync(Guid proposalId, string approvedBy) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var proposal = await db.Proposals.Include(p => p.ReserveStudy)!
            .ThenInclude(s => s.Community)
            .FirstOrDefaultAsync(p => p.Id == proposalId);
            if (proposal?.ReserveStudy == null) return false;

            proposal.IsApproved = true;
            proposal.ApprovedBy = approvedBy;
            proposal.DateApproved = DateTime.UtcNow;

            var study = proposal.ReserveStudy;

            // Enforce transition -> ProposalApproved => Approved
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.Approved;
            var ok = await _engine.TryTransitionAsync(sr, desired, approvedBy);
            if (!ok) return false;

            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;

            await AppendHistoryAsync(db, sr, from, desired, approvedBy);
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new ProposalApprovedEvent(study, proposal));
            return true;
        }

        public async Task<bool> RequestFinancialInfoAsync(Guid reserveStudyId) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var study = await db.ReserveStudies.Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;

            // Enforce transition -> FinancialInfoRequested => NeedsInfo
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.NeedsInfo;
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new FinancialInfoRequestedEvent(study));
            return true;
        }

        public async Task<bool> SubmitFinancialInfoAsync(Guid reserveStudyId, FinancialInfo financialInfo) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var study = await db.ReserveStudies.Include(s => s.Community)
            .Include(s => s.FinancialInfo)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;

            financialInfo.ReserveStudyId = study.Id;
            financialInfo.TenantId = study.TenantId;

            if (study.FinancialInfo == null) {
                await db.FinancialInfos.AddAsync(financialInfo);
                study.FinancialInfo = financialInfo;
            } else {
                // Update known fields (add others if needed in model)
                study.FinancialInfo.CurrentReserveFundBalance = financialInfo.CurrentReserveFundBalance;
                study.FinancialInfo.AnnualContribution = financialInfo.AnnualContribution;
                study.FinancialInfo.ProjectedAnnualExpenses = financialInfo.ProjectedAnnualExpenses;
                study.FinancialInfo.FiscalYearStartMonth = financialInfo.FiscalYearStartMonth;
                study.FinancialInfo.FinancialDocumentUrls = financialInfo.FinancialDocumentUrls;
                study.FinancialInfo.Comments = financialInfo.Comments;
            }

            // Enforce transition -> FinancialInfoSubmitted => UnderReview
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.UnderReview;
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new FinancialInfoSubmittedEvent(study, study.FinancialInfo ?? financialInfo));
            return true;
        }

        public async Task<bool> ReviewFinancialInfoAsync(Guid financialInfoId, string reviewedBy) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var fin = await db.FinancialInfos.Include(f => f.ReserveStudy)!
            .ThenInclude(s => s.Community)
            .FirstOrDefaultAsync(f => f.Id == financialInfoId);
            if (fin?.ReserveStudy == null) return false;

            // Try to set DateReviewed if available on model (ignore if not)
            try {
                var prop = fin.GetType().GetProperty("DateReviewed");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(fin, DateTime.UtcNow);
            } catch { /* ignore missing */ }

            var study = fin.ReserveStudy;

            // Enforce transition -> FinancialInfoReviewed => ReportDrafted
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.ReportDrafted;
            var ok = await _engine.TryTransitionAsync(sr, desired, reviewedBy);
            if (!ok) return false;

            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, desired, reviewedBy);
            await db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteReserveStudyAsync(Guid reserveStudyId) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var study = await db.ReserveStudies.Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;

            // Enforce transition -> RequestCompleted => Complete
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var desired = StudyStatus.Complete;
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            study.IsComplete = true;
            study.Status = StatusMapper.ToLegacy(desired);
            study.LastModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new ReserveStudyCompletedEvent(study));
            return true;
        }

        // Legacy helpers retained for compatibility
        public async Task<string[]> GetAllowedLegacyTransitionsAsync(Guid reserveStudyId) {
            var studyAllowed = await GetAllowedStudyTransitionsAsync(reserveStudyId);
            return studyAllowed.Select(s => StatusMapper.ToLegacy(s).ToString()).ToArray();
        }

        public async Task<bool> TryTransitionLegacyAsync(Guid reserveStudyId, ReserveStudy.WorkflowStatus targetStatus, string? actor = null) {
            var desired = StatusMapper.ToStudyStatus(targetStatus);
            return await TryTransitionStudyAsync(reserveStudyId, desired, actor);
        }

        // StudyStatus helpers
        public async Task<StudyStatus[]> GetAllowedStudyTransitionsAsync(Guid reserveStudyId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var study = await db.ReserveStudies.AsNoTracking().FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return Array.Empty<StudyStatus>();
            var sr = await EnsureStudyRequestAsync(db, study);
            var current = sr.CurrentStatus;
            var all = Enum.GetValues(typeof(StudyStatus)).Cast<StudyStatus>();
            return all.Where(to => _engine.IsTransitionAllowed(current, to)).ToArray();
        }

        public async Task<bool> TryTransitionStudyAsync(Guid reserveStudyId, StudyStatus targetStatus, string? actor = null) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var study = await db.ReserveStudies.Include(s => s.Community).FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            if (!_engine.IsTransitionAllowed(from, targetStatus)) return false;
            var ok = await _engine.TryTransitionAsync(sr, targetStatus, actor ?? GetActor());
            if (!ok) return false;
            study.Status = StatusMapper.ToLegacy(targetStatus);
            study.LastModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, targetStatus, actor ?? GetActor());
            await db.SaveChangesAsync();
            return true;
        }

        private static StudyRequest ToStudyRequest(ReserveStudy study) {
            return new StudyRequest {
                Id = study.Id, //1:1 key by ReserveStudy.Id
                TenantId = study.TenantId,
                CommunityId = study.CommunityId ?? Guid.Empty,
                CurrentStatus = StatusMapper.ToStudyStatus(study.Status),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                StateChangedAt = DateTimeOffset.UtcNow
            };
        }

        private async Task<StudyRequest> EnsureStudyRequestAsync(ApplicationDbContext db, ReserveStudy study) {
            var sr = await db.StudyRequests.FirstOrDefaultAsync(x => x.Id == study.Id);
            if (sr == null) {
                sr = ToStudyRequest(study);
                await db.StudyRequests.AddAsync(sr);
                // initial history row
                await AppendHistoryAsync(db, sr, sr.CurrentStatus, sr.CurrentStatus, GetActor());
                await db.SaveChangesAsync();
            }
            return sr;
        }

        private string? GetActor() => _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        private string GetSource() {
            var path = _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? string.Empty;
            return path.Contains("/api/", StringComparison.OrdinalIgnoreCase) ? "API" : "UI";
        }

        private string? GetCorrelationId() {
            var headers = _httpContextAccessor.HttpContext?.Request?.Headers;
            if (headers != null && headers.TryGetValue("X-Correlation-Id", out var values)) {
                return values.FirstOrDefault();
            }
            return null;
        }

        private Task AppendHistoryAsync(ApplicationDbContext db, StudyRequest sr, StudyStatus from, StudyStatus to, string? actor) {
            var history = new StudyStatusHistory {
                TenantId = sr.TenantId,
                RequestId = sr.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedBy = actor,
                Source = GetSource(),
                CorrelationId = GetCorrelationId()
            };
            db.StudyStatusHistories.Add(history);
            return Task.CompletedTask;
        }
    }
}