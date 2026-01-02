using System;
using System.Threading.Tasks;

using CRS.Models.Workflow;

namespace CRS.Services.Interfaces {
    /// <summary>
    /// Defines the operations to validate and perform workflow transitions for StudyRequest.
    /// Encapsulates the allowed state transitions and emits notifications on changes.
    /// </summary>
    public interface IStudyWorkflowService {
        /// <summary>
        /// Attempts to transition a request to a new state. Returns false if transition is not allowed.
        /// Updates timestamps and invokes notification hooks on success.
        /// </summary>
        /// <param name="request">The tracked study request entity.</param>
        /// <param name="newStatus">The desired target status.</param>
        /// <param name="actor">Optional actor identifier (user name/id) performing the transition.</param>
        Task<bool> TryTransitionAsync(StudyRequest request, StudyStatus newStatus, string? actor = null);

        /// <summary>
        /// Checks if a transition from the current state to the target state is allowed by the state machine.
        /// </summary>
        /// <param name="from">The current state.</param>
        /// <param name="to">The desired target state.</param>
        /// <returns>True if transition is allowed; otherwise false.</returns>
        bool IsTransitionAllowed(StudyStatus from, StudyStatus to);

        /// <summary>
        /// Checks if an admin override transition is allowed.
        /// </summary>
        bool IsAdminTransitionAllowed(StudyStatus from, StudyStatus to);

        /// <summary>
        /// Gets all statuses that can be transitioned to from the current status.
        /// </summary>
        StudyStatus[] GetAllowedTransitions(StudyStatus from);

        /// <summary>
        /// Gets all possible statuses for admin skip/rollback operations.
        /// </summary>
        StudyStatus[] GetAllStatuses();

        /// <summary>
        /// Forces a transition regardless of allowed transitions (admin only).
        /// </summary>
        Task<bool> ForceTransitionAsync(StudyRequest request, StudyStatus newStatus, string? actor = null);
    }
}
