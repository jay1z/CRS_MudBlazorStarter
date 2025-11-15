using CRS.Models;
using CRS.Models.Workflow;

namespace CRS.Services.Workflow {
    public static class StatusMapper {
        public static StudyStatus ToStudyStatus(ReserveStudy.WorkflowStatus legacy) {
            return legacy switch {
                ReserveStudy.WorkflowStatus.RequestCreated => StudyStatus.NewRequest,
                ReserveStudy.WorkflowStatus.ProposalCreated => StudyStatus.Assigned,
                ReserveStudy.WorkflowStatus.ProposalReviewed => StudyStatus.ReadyForReview,
                ReserveStudy.WorkflowStatus.ProposalApproved => StudyStatus.Approved,
                ReserveStudy.WorkflowStatus.ProposalSent => StudyStatus.ProposalPendingESign,
                ReserveStudy.WorkflowStatus.ProposalAccepted => StudyStatus.Accepted,
                ReserveStudy.WorkflowStatus.FinancialInfoRequested => StudyStatus.NeedsInfo,
                ReserveStudy.WorkflowStatus.FinancialInfoCreated => StudyStatus.UnderReview,
                ReserveStudy.WorkflowStatus.FinancialInfoSubmitted => StudyStatus.UnderReview,
                ReserveStudy.WorkflowStatus.FinancialInfoReviewed => StudyStatus.ReportDrafted,
                ReserveStudy.WorkflowStatus.SiteVisitScheduled => StudyStatus.Scheduled,
                ReserveStudy.WorkflowStatus.ReportInProcess => StudyStatus.InProgress,
                ReserveStudy.WorkflowStatus.ReportComplete => StudyStatus.Complete,
                ReserveStudy.WorkflowStatus.RequestCompleted => StudyStatus.Complete,
                ReserveStudy.WorkflowStatus.RequestArchived => StudyStatus.Archived,
                _ => StudyStatus.NewRequest
            };
        }

        public static ReserveStudy.WorkflowStatus ToLegacy(StudyStatus status) {
            return status switch {
                StudyStatus.NewRequest => ReserveStudy.WorkflowStatus.RequestCreated,
                StudyStatus.PendingDetails => ReserveStudy.WorkflowStatus.RequestCreated,
                StudyStatus.ReadyForReview => ReserveStudy.WorkflowStatus.ProposalReviewed,
                StudyStatus.NeedsInfo => ReserveStudy.WorkflowStatus.FinancialInfoRequested,
                StudyStatus.Approved => ReserveStudy.WorkflowStatus.ProposalApproved,
                StudyStatus.Assigned => ReserveStudy.WorkflowStatus.ProposalCreated,
                StudyStatus.ProposalPendingESign => ReserveStudy.WorkflowStatus.ProposalSent,
                StudyStatus.Accepted => ReserveStudy.WorkflowStatus.ProposalAccepted,
                StudyStatus.Rejected => ReserveStudy.WorkflowStatus.RequestCancelled,
                StudyStatus.Scheduled => ReserveStudy.WorkflowStatus.SiteVisitScheduled,
                StudyStatus.InProgress => ReserveStudy.WorkflowStatus.ReportInProcess,
                StudyStatus.UnderReview => ReserveStudy.WorkflowStatus.FinancialInfoCreated,
                StudyStatus.ReportDrafted => ReserveStudy.WorkflowStatus.FinancialInfoReviewed,
                StudyStatus.ApprovedReport => ReserveStudy.WorkflowStatus.ReportReady,
                StudyStatus.Complete => ReserveStudy.WorkflowStatus.RequestCompleted,
                StudyStatus.Archived => ReserveStudy.WorkflowStatus.RequestArchived,
                _ => ReserveStudy.WorkflowStatus.RequestCreated
            };
        }
    }
}
