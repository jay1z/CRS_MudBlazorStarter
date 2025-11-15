using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CRS.Models.Workflow;
using CRS.Services.Interfaces;

namespace CRS.Services.Workflow {
    /// <summary>
    /// Pure state-machine service to manage StudyRequest transitions.
    /// </summary>
    public class StudyWorkflowService : IStudyWorkflowService {
        private readonly INotificationService _notification;

        // Allowed transitions graph
        private static readonly Dictionary<StudyStatus, HashSet<StudyStatus>> _allowed = new() {
            [StudyStatus.NewRequest] = new() { StudyStatus.PendingDetails },
            [StudyStatus.PendingDetails] = new() { StudyStatus.ReadyForReview },
            [StudyStatus.ReadyForReview] = new() { StudyStatus.Approved, StudyStatus.NeedsInfo },
            [StudyStatus.NeedsInfo] = new() { StudyStatus.ReadyForReview },
            [StudyStatus.Approved] = new() { StudyStatus.Assigned },
            [StudyStatus.Assigned] = new() { StudyStatus.ProposalPendingESign },
            [StudyStatus.ProposalPendingESign] = new() { StudyStatus.Accepted, StudyStatus.Rejected },
            [StudyStatus.Accepted] = new() { StudyStatus.Scheduled },
            [StudyStatus.Rejected] = new() { },
            [StudyStatus.Scheduled] = new() { StudyStatus.InProgress },
            [StudyStatus.InProgress] = new() { StudyStatus.UnderReview },
            [StudyStatus.UnderReview] = new() { StudyStatus.ReportDrafted, StudyStatus.InProgress },
            [StudyStatus.ReportDrafted] = new() { StudyStatus.ApprovedReport },
            [StudyStatus.ApprovedReport] = new() { StudyStatus.Complete },
            [StudyStatus.Complete] = new() { StudyStatus.Archived },
            [StudyStatus.Archived] = new() { }
        };

        public StudyWorkflowService(INotificationService notification) {
            _notification = notification;
        }

        public bool IsTransitionAllowed(StudyStatus from, StudyStatus to) {
            return _allowed.TryGetValue(from, out var set) && set.Contains(to);
        }

        public async Task<bool> TryTransitionAsync(StudyRequest request, StudyStatus newStatus, string? actor = null) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var old = request.CurrentStatus;
            if (!IsTransitionAllowed(old, newStatus)) return false;

            request.CurrentStatus = newStatus;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.StateChangedAt = request.UpdatedAt;
            request.StatusChangedBy = actor;

            await _notification.OnStateChangedAsync(request, old, newStatus);
            return true;
        }
    }
}
