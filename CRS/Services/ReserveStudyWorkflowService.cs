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
using Microsoft.Extensions.Logging;

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
        private readonly IScopeComparisonService _scopeComparisonService;
        private readonly ILogger<ReserveStudyWorkflowService> _logger;

        public ReserveStudyWorkflowService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IReserveStudyService reserveStudyService,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IStudyWorkflowService engine,
        INotificationService notifier,
        IDispatcher dispatcher,
        IScopeComparisonService scopeComparisonService,
        ILogger<ReserveStudyWorkflowService> logger) {
            _dbFactory = dbFactory;
            _reserveStudyService = reserveStudyService;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
            _engine = engine;
            _notifier = notifier;
            _dispatcher = dispatcher;
            _scopeComparisonService = scopeComparisonService;
            _logger = logger;
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
            .Include(s => s.Proposals)
            .Include(s => s.CurrentProposal)
            .Include(s => s.Community)
            .Include(s => s.User)
            .Include(s => s.Contact)
            .Include(s => s.PropertyManager)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;

            // Upsert proposal
            proposal.ReserveStudyId = study.Id;
            proposal.TenantId = study.TenantId;
            var isNewProposal = study.CurrentProposalId == null;
            var isAmendment = proposal.IsAmendment;
            
            if (isNewProposal) {
                // Generate ID for new proposal if not set
                if (proposal.Id == Guid.Empty) {
                    proposal.Id = Guid.CreateVersion7();
                }
                db.Proposals.Add(proposal);
                // Set as current proposal
                study.CurrentProposalId = proposal.Id;
            } else if (isAmendment) {
                // For amendments, create a new proposal record linked to the original
                var originalProposalId = study.CurrentProposalId!.Value;
                
                // Generate new ID for amendment
                if (proposal.Id == Guid.Empty) {
                    proposal.Id = Guid.CreateVersion7();
                }
                proposal.OriginalProposalId = originalProposalId;
                proposal.DateSent = DateTime.UtcNow;
                
                // Add the new amendment proposal
                db.Proposals.Add(proposal);
                
                // Update current proposal to the amendment
                study.CurrentProposalId = proposal.Id;
                study.DateModified = DateTime.UtcNow;
                
                try {
                    await db.SaveChangesAsync();
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error saving proposal amendment for study {StudyId}", reserveStudyId);
                    throw;
                }
                
                // Reload study with new proposal for event
                study = await db.ReserveStudies
                    .Include(s => s.CurrentProposal)
                    .Include(s => s.Community)
                    .Include(s => s.Contact)
                    .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
                
                // Fire event for email notification
                await _dispatcher.Broadcast(new ProposalSentEvent(study!, proposal));
                return true;
            } else {
                // Update existing proposal (not an amendment)
                var currentProposal = study.CurrentProposal;
                if (currentProposal != null) {
                    currentProposal.ProposalScope = proposal.ProposalScope;
                    currentProposal.EstimatedCost = proposal.EstimatedCost;
                    currentProposal.Comments = proposal.Comments;
                    currentProposal.ServiceLevel = proposal.ServiceLevel;
                    currentProposal.DeliveryTimeframe = proposal.DeliveryTimeframe;
                    currentProposal.PaymentTerms = proposal.PaymentTerms;
                    currentProposal.IncludePrepaymentDiscount = proposal.IncludePrepaymentDiscount;
                    currentProposal.IncludeDigitalDelivery = proposal.IncludeDigitalDelivery;
                    currentProposal.IncludeComponentInventory = proposal.IncludeComponentInventory;
                    currentProposal.IncludeFundingPlans = proposal.IncludeFundingPlans;
                }
            }

            // Determine target status based on current workflow state
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            
            // If creating a new proposal from RequestApproved, transition to ProposalCreated
            // If we're at ProposalApproved and ready to send, transition to ProposalSent
            StudyStatus desired;
            if (from == StudyStatus.RequestApproved && isNewProposal) {
                desired = StudyStatus.ProposalCreated;
            } else if (from == StudyStatus.ProposalApproved) {
                desired = StudyStatus.ProposalSent;
                // Set DateSent when actually sending
                if (study.CurrentProposal != null) {
                    study.CurrentProposal.DateSent = DateTime.UtcNow;
                }
            } else if (from == StudyStatus.ProposalCreated || from == StudyStatus.ProposalUpdated) {
                // Updating proposal but not sending yet - just save without transition
                study.DateModified = DateTime.UtcNow;
                try {
                    await db.SaveChangesAsync();
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error updating proposal for study {StudyId}", reserveStudyId);
                    throw;
                }
                return true;
            } else {
                // Unknown state - try to proceed with the most logical transition
                desired = isNewProposal ? StudyStatus.ProposalCreated : StudyStatus.ProposalSent;
            }
            
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            study.DateModified = DateTime.UtcNow;

            // History row
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            try {
                await db.SaveChangesAsync();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error saving proposal workflow transition for study {StudyId}", reserveStudyId);
                throw;
            }

            // Fire event for email workflows/listeners only when actually sending to client
            if (desired == StudyStatus.ProposalSent) {
                await _dispatcher.Broadcast(new ProposalSentEvent(study, study.CurrentProposal ?? proposal));
            }
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
            var desired = StudyStatus.ProposalApproved;
            var ok = await _engine.TryTransitionAsync(sr, desired, approvedBy);
            if (!ok) return false;

                        study.DateModified = DateTime.UtcNow;

            await AppendHistoryAsync(db, sr, from, desired, approvedBy);
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new ProposalApprovedEvent(study, proposal));
            return true;
        }

        public async Task<bool> ResendProposalEmailAsync(Guid reserveStudyId) {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var study = await db.ReserveStudies
                .Include(s => s.CurrentProposal)
                .Include(s => s.Community)
                .Include(s => s.User)
                .Include(s => s.Contact)
                .Include(s => s.PropertyManager)
                .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            
            if (study?.CurrentProposal == null) return false;

            // Only allow resending if proposal has already been sent
            var sr = await db.StudyRequests.FirstOrDefaultAsync(x => x.Id == reserveStudyId);
            if (sr == null || sr.CurrentStatus < StudyStatus.ProposalSent) return false;

            // Don't allow resending if already accepted
            if (sr.CurrentStatus >= StudyStatus.ProposalAccepted) return false;

            // Update the DateSent to track when it was last sent
            study.CurrentProposal.DateSent = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Fire the event to send the email
            await _dispatcher.Broadcast(new ProposalSentEvent(study, study.CurrentProposal));
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
            var desired = StudyStatus.FinancialInfoRequested;
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

                        study.DateModified = DateTime.UtcNow;
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
            financialInfo.DateSubmitted = DateTime.UtcNow;

            if (study.FinancialInfo == null) {
                await db.FinancialInfos.AddAsync(financialInfo);
                study.FinancialInfo = financialInfo;
            } else {
                // Update all fields from the new comprehensive form
                study.FinancialInfo.JanuaryFirstReserveBalance = financialInfo.JanuaryFirstReserveBalance;
                study.FinancialInfo.DecemberThirtyFirstReserveBalance = financialInfo.DecemberThirtyFirstReserveBalance;
                study.FinancialInfo.BudgetedContributionLastYear = financialInfo.BudgetedContributionLastYear;
                study.FinancialInfo.BudgetedContributionCurrentYear = financialInfo.BudgetedContributionCurrentYear;
                study.FinancialInfo.BudgetedContributionNextYear = financialInfo.BudgetedContributionNextYear;
                study.FinancialInfo.OperatingBudgetCurrentYear = financialInfo.OperatingBudgetCurrentYear;
                study.FinancialInfo.OperatingBudgetNextYear = financialInfo.OperatingBudgetNextYear;
                study.FinancialInfo.TotalNumberOfUnits = financialInfo.TotalNumberOfUnits;
                study.FinancialInfo.AnnualMeetingDate = financialInfo.AnnualMeetingDate;
                study.FinancialInfo.FiscalYearStartMonth = financialInfo.FiscalYearStartMonth;
                study.FinancialInfo.LoanAmount = financialInfo.LoanAmount;
                study.FinancialInfo.LoanBalanceRemaining = financialInfo.LoanBalanceRemaining;
                study.FinancialInfo.LoanExpectedYearComplete = financialInfo.LoanExpectedYearComplete;
                study.FinancialInfo.SpecialAssessmentAmount = financialInfo.SpecialAssessmentAmount;
                study.FinancialInfo.SpecialAssessmentBalanceRemaining = financialInfo.SpecialAssessmentBalanceRemaining;
                study.FinancialInfo.SpecialAssessmentExpectedYearComplete = financialInfo.SpecialAssessmentExpectedYearComplete;
                study.FinancialInfo.PlannedProjects = financialInfo.PlannedProjects;
                study.FinancialInfo.PropertyInsuranceDeductible = financialInfo.PropertyInsuranceDeductible;
                study.FinancialInfo.InterestRateOnReserveFunds = financialInfo.InterestRateOnReserveFunds;
                study.FinancialInfo.BuildingRoofSidingInfo = financialInfo.BuildingRoofSidingInfo;
                study.FinancialInfo.ComponentReplacementDates = financialInfo.ComponentReplacementDates;
                study.FinancialInfo.SidingCalculationPreference = financialInfo.SidingCalculationPreference;
                study.FinancialInfo.AcknowledgementAccepted = financialInfo.AcknowledgementAccepted;
                study.FinancialInfo.CommunityNameOnAcknowledgment = financialInfo.CommunityNameOnAcknowledgment;
                study.FinancialInfo.PresidentSignature = financialInfo.PresidentSignature;
                study.FinancialInfo.AcknowledgmentSignatureDate = financialInfo.AcknowledgmentSignatureDate;
                study.FinancialInfo.FinancialDocumentUrls = financialInfo.FinancialDocumentUrls;
                study.FinancialInfo.Comments = financialInfo.Comments;
                study.FinancialInfo.DateSubmitted = DateTime.UtcNow;
                
                // Legacy fields
                study.FinancialInfo.CurrentReserveFundBalance = financialInfo.CurrentReserveFundBalance;
                study.FinancialInfo.AnnualContribution = financialInfo.AnnualContribution;
                study.FinancialInfo.ProjectedAnnualExpenses = financialInfo.ProjectedAnnualExpenses;
            }

            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            var actor = GetActor();

            // Handle the workflow transition properly:
            // If status is FinancialInfoRequested, first transition to InProgress, then to Submitted
            // If status is already FinancialInfoInProgress, just transition to Submitted
            if (sr.CurrentStatus == StudyStatus.FinancialInfoRequested) {
                // First transition to InProgress
                var okInProgress = await _engine.TryTransitionAsync(sr, StudyStatus.FinancialInfoInProgress, actor);
                if (!okInProgress) return false;
                await AppendHistoryAsync(db, sr, from, StudyStatus.FinancialInfoInProgress, actor);
                from = StudyStatus.FinancialInfoInProgress;
            }

            // Now transition to Submitted
            if (sr.CurrentStatus == StudyStatus.FinancialInfoInProgress) {
                var okSubmitted = await _engine.TryTransitionAsync(sr, StudyStatus.FinancialInfoSubmitted, actor);
                if (!okSubmitted) return false;
                await AppendHistoryAsync(db, sr, from, StudyStatus.FinancialInfoSubmitted, actor);
            }

            study.DateModified = DateTime.UtcNow;
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
            var desired = StudyStatus.ReportReady;
            var ok = await _engine.TryTransitionAsync(sr, desired, reviewedBy);
            if (!ok) return false;

                        study.DateModified = DateTime.UtcNow;
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
            var desired = StudyStatus.RequestCompleted;
            var ok = await _engine.TryTransitionAsync(sr, desired, GetActor());
            if (!ok) return false;

            study.IsComplete = true;
                        study.DateModified = DateTime.UtcNow;
            await AppendHistoryAsync(db, sr, from, desired, GetActor());
            await db.SaveChangesAsync();

            await _dispatcher.Broadcast(new ReserveStudyCompletedEvent(study));
            return true;
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
            var study = await db.ReserveStudies
                .Include(s => s.Community)
                .Include(s => s.Proposal)
                .Include(s => s.User)
                .Include(s => s.Contact)
                .Include(s => s.PropertyManager)
                .FirstOrDefaultAsync(s => s.Id == reserveStudyId);
            if (study == null) return false;
            var sr = await EnsureStudyRequestAsync(db, study);
            var from = sr.CurrentStatus;
            if (!_engine.IsTransitionAllowed(from, targetStatus)) return false;
            var ok = await _engine.TryTransitionAsync(sr, targetStatus, actor ?? GetActor());
            if (!ok) return false;
            study.DateModified = DateTime.UtcNow;
            
            // Set DateSent when transitioning to ProposalSent
            if (targetStatus == StudyStatus.ProposalSent && study.Proposal != null) {
                study.Proposal.DateSent = DateTime.UtcNow;
            }
            
            await AppendHistoryAsync(db, sr, from, targetStatus, actor ?? GetActor());
            await db.SaveChangesAsync();
            
            // Fire events for specific transitions
            if (targetStatus == StudyStatus.ProposalSent && study.Proposal != null) {
                await _dispatcher.Broadcast(new ProposalSentEvent(study, study.Proposal));
            }
            
            // Trigger scope comparison when site visit data entry is complete
            if (targetStatus == StudyStatus.SiteVisitDataEntered) {
                try {
                    var userId = GetCurrentUserId();
                    var comparisonResult = await _scopeComparisonService.CompareAndEvaluateAsync(reserveStudyId, userId);
                    
                    if (comparisonResult.IsSuccess && comparisonResult.ExceedsThreshold) {
                        // Log variance detection - the UI should display this to the user
                        // The service determines if workflow should block based on tenant settings
                        // For now, we log and continue - blocking logic will be handled by StudyDataValidationResult
                    }
                } catch (Exception) {
                    // Scope comparison failure should not block workflow transition
                    // The comparison record may not exist if proposal wasn't accepted through the standard flow
                }
            }
            
            return true;
        }

        private Guid GetCurrentUserId() {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private static StudyRequest ToStudyRequest(ReserveStudy study) {
            return new StudyRequest {
                Id = study.Id, //1:1 key by ReserveStudy.Id
                TenantId = study.TenantId,
                CommunityId = study.CommunityId ?? Guid.Empty,
                CurrentStatus = study.CurrentStatus,
                // Capture initial element counts as estimates
                EstimatedBuildingElementCount = study.ReserveStudyBuildingElements?.Count ?? 0,
                EstimatedCommonElementCount = study.ReserveStudyCommonElements?.Count ?? 0,
                EstimatedAdditionalElementCount = study.ReserveStudyAdditionalElements?.Count ?? 0,
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