You are building backend workflow logic for a multi-tenant Blazor Server SaaS platform
that manages Reserve Studies for HOAs. Each tenant is a Reserve Study company that handles
its own studies, while the SaaS platform manages workflow, notifications, and automation.

==========================================================
OBJECTIVE
==========================================================
Create a state-driven workflow service that manages the lifecycle
of a Reserve Study request from submission through report delivery
and archival. Each state transition should trigger notifications,
enforce allowed transitions, and record timestamps. The code
should be modular and support multi-tenancy.

==========================================================
ENTITIES
==========================================================
Tenant – the Reserve Study company (has its own users, specialists, and customers)
HOAUser (Board Member) – creates requests, signs proposals
StudyRequest – represents a single study request, moves through workflow states
Specialist – performs inspections and uploads photos/notes
NotificationService – sends email/SMS on state change
StudyWorkflowService – enforces state transitions and triggers automation

==========================================================
ENUM: StudyStatus
==========================================================
Create an enum that includes the following workflow states:
NewRequest, PendingDetails, ReadyForReview, NeedsInfo, Approved,
Assigned, ProposalPendingESign, Accepted, Rejected, Scheduled,
InProgress, UnderReview, ReportDrafted, ApprovedReport,
Complete, Archived.

==========================================================
STATE MACHINE TRANSITIONS
==========================================================
NewRequest -> PendingDetails         (when board member submits form)
PendingDetails -> ReadyForReview     (when all elements completed)
ReadyForReview -> Approved / NeedsInfo
Approved -> Assigned                 (tenant assigns specialist)
Assigned -> ProposalPendingESign     (specialist drafts proposal)
ProposalPendingESign -> Accepted / Rejected (board signs or rejects)
Accepted -> Scheduled                (tenant schedules appointment)
Scheduled -> InProgress              (inspection starts)
InProgress -> UnderReview            (data uploaded)
UnderReview -> ReportDrafted / InProgress
ReportDrafted -> ApprovedReport      (QA complete)
ApprovedReport -> Complete           (report delivered)
Complete -> Archived                 (auto archive + renewal reminder)

==========================================================
AUTOMATION RULES
==========================================================
- Every state change triggers NotificationService.Send()
- ProposalPendingESign sends 24h and 72h reminders if unsigned
- NeedsInfo and UnderReview send daily reminders after 5 days
- Archived schedules an automatic 3-year renewal reminder
- Each StudyRequest stores timestamps for CreatedAt, UpdatedAt, and StateChangedAt

==========================================================
IMPLEMENTATION REQUIREMENTS
==========================================================
1. Create StudyStatus enum.
2. Create StudyRequest class with TenantId, HOAId, CurrentStatus, and timestamps.
3. Create IStudyWorkflowService interface defining allowed transitions and validation.
4. Create StudyWorkflowService class that:
   - Validates state transitions using a dictionary or switch statement.
   - Calls NotificationService.OnStateChanged(StudyRequest, oldState, newState).
   - Logs transition timestamps.
5. Create NotificationService with methods for Send(), SendReminder(), and ScheduleRenewal().
6. Use dependency injection (register services in Program.cs).
7. Optionally, integrate background jobs (Hangfire or Quartz.NET) for scheduled reminders.
8. Keep methods async, using Task.Run or background worker for long-running tasks.

==========================================================
TASK FOR COPILOT
==========================================================
Generate all necessary enums, classes, and services (StudyStatus, StudyRequest,
IStudyWorkflowService, StudyWorkflowService, NotificationService).
Include XML comments for each state and method explaining purpose and transitions.
Provide example usage demonstrating a transition and notification trigger.
The resulting code should compile and run under .NET 8 Blazor Server.

