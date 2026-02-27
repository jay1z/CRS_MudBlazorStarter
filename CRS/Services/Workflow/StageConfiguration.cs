using Horizon.Models.Workflow;

namespace Horizon.Services.Workflow;

/// <summary>
/// Defines the available actions for a workflow stage.
/// </summary>
[Flags]
public enum StageAction
{
    None = 0,
    Advance = 1,
    Reject = 2,
    RequestChanges = 4,
    Cancel = 8,
    Skip = 16,        // Admin only
    MoveBack = 32,    // Admin only
    ForceComplete = 64 // Admin only
}

/// <summary>
/// Defines who can perform actions on a stage.
/// </summary>
[Flags]
public enum StageActor
{
    None = 0,
    HOA = 1,
    Staff = 2,
    Specialist = 4,
    Admin = 8,
    System = 16
}

/// <summary>
/// Defines validation requirements for advancing a stage.
/// </summary>
public enum StageValidation
{
    None,
    ProposalDocumentAttached,
    ProposalReviewed,
    ESignEnvelopeCreated,
    SignedAcceptanceReceived,
    ServiceContactsProvided,
    FinancialInfoFormSent,
    FinancialInfoComplete,
    FinancialInfoReviewed,
    SiteVisitDateSelected,
    SiteVisitPhotosUploaded,
    SiteVisitDataComplete,
    FundingPlanCreated,
    FundingPlanComplete,
    NarrativeCreated,
    NarrativeComplete,
    ReportAttached,
    ReportApproved,
    CancellationReasonProvided
}

/// <summary>
/// Configuration metadata for a single workflow stage.
/// </summary>
public class StageConfig
{
    public StudyStatus Status { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    
    /// <summary>
    /// Who can advance this stage to the next.
    /// </summary>
    public StageActor AdvancedBy { get; init; } = StageActor.Staff;
    
    /// <summary>
    /// Actions available at this stage.
    /// </summary>
    public StageAction AvailableActions { get; init; } = StageAction.Advance;
    
    /// <summary>
    /// Validation required before advancing.
    /// </summary>
    public StageValidation[] RequiredValidations { get; init; } = Array.Empty<StageValidation>();
    
    /// <summary>
    /// If true, automatically advances when validation passes.
    /// </summary>
    public bool AutoAdvance { get; init; } = false;
    
    /// <summary>
    /// The default next stage when advancing.
    /// </summary>
    public StudyStatus? DefaultNextStage { get; init; }
    
    /// <summary>
    /// Alternative stages that can be transitioned to (e.g., reject goes here).
    /// </summary>
    public Dictionary<StageAction, StudyStatus> AlternativeTransitions { get; init; } = new();
    
    /// <summary>
    /// Roles to notify when entering this stage.
    /// </summary>
    public StageActor NotifyOnEnter { get; init; } = StageActor.None;
    
    /// <summary>
    /// Roles to notify when leaving this stage.
    /// </summary>
    public StageActor NotifyOnExit { get; init; } = StageActor.None;
    
    /// <summary>
    /// Message to show the user about what's needed.
    /// </summary>
    public string ActionRequiredMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// Message to show when waiting on another role.
    /// </summary>
    public string WaitingMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// Can run in parallel with other stages (for data collection).
    /// </summary>
    public StudyStatus[] ParallelWith { get; init; } = Array.Empty<StudyStatus>();
}

/// <summary>
/// Central configuration for all workflow stages.
/// </summary>
public static class StageConfiguration
{
    private static readonly Dictionary<StudyStatus, StageConfig> _configs = new()
    {
        // ═══════════════════════════════════════════════════════════════
        // REQUEST PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.RequestCreated] = new StageConfig
        {
            Status = StudyStatus.RequestCreated,
            DisplayName = "Request Created",
            Phase = "Request",
            AdvancedBy = StageActor.Staff | StageActor.Admin,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.RequestApproved,
            NotifyOnEnter = StageActor.Staff,
            ActionRequiredMessage = "Review the request and approve for proposal creation.",
            WaitingMessage = "Your request is being reviewed by our team."
        },

        [StudyStatus.RequestApproved] = new StageConfig
        {
            Status = StudyStatus.RequestApproved,
            DisplayName = "Request Approved",
            Phase = "Request",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.ProposalCreated,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Assign a specialist and begin creating the proposal.",
            WaitingMessage = "Your request has been approved. A specialist will be assigned shortly."
        },

        // ═══════════════════════════════════════════════════════════════
        // PROPOSAL PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.ProposalCreated] = new StageConfig
        {
            Status = StudyStatus.ProposalCreated,
            DisplayName = "Proposal Created",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ProposalDocumentAttached },
            DefaultNextStage = StudyStatus.ProposalReviewed,
            NotifyOnEnter = StageActor.Staff,
            ActionRequiredMessage = "Attach proposal document and submit for review.",
            WaitingMessage = "A proposal is being prepared for your project."
        },

        [StudyStatus.ProposalReviewed] = new StageConfig
        {
            Status = StudyStatus.ProposalReviewed,
            DisplayName = "Proposal Under Review",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Admin,
            AvailableActions = StageAction.Advance | StageAction.RequestChanges | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ProposalReviewed },
            DefaultNextStage = StudyStatus.ProposalApproved,
            AlternativeTransitions = new() { [StageAction.RequestChanges] = StudyStatus.ProposalUpdated },
            NotifyOnEnter = StageActor.Admin,
            ActionRequiredMessage = "Review the proposal and approve or request changes.",
            WaitingMessage = "The proposal is under internal review."
        },

        [StudyStatus.ProposalUpdated] = new StageConfig
        {
            Status = StudyStatus.ProposalUpdated,
            DisplayName = "Proposal Updated",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ProposalDocumentAttached },
            DefaultNextStage = StudyStatus.ProposalReviewed,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Update the proposal based on feedback and resubmit.",
            WaitingMessage = "The proposal is being updated."
        },

        [StudyStatus.ProposalApproved] = new StageConfig
        {
            Status = StudyStatus.ProposalApproved,
            DisplayName = "Proposal Approved",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ESignEnvelopeCreated },
            DefaultNextStage = StudyStatus.ProposalSent,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Create e-sign envelope and send proposal to client.",
            WaitingMessage = "The proposal has been approved and will be sent shortly."
        },

        [StudyStatus.ProposalSent] = new StageConfig
        {
            Status = StudyStatus.ProposalSent,
            DisplayName = "Proposal Sent",
            Phase = "Proposal",
            AdvancedBy = StageActor.HOA, // HOA accepts
            AvailableActions = StageAction.Advance | StageAction.Reject,
            RequiredValidations = new[] { StageValidation.SignedAcceptanceReceived },
            DefaultNextStage = StudyStatus.ProposalAccepted,
            AlternativeTransitions = new() { 
                [StageAction.Reject] = StudyStatus.ProposalDeclined 
            },
            NotifyOnEnter = StageActor.HOA,
            NotifyOnExit = StageActor.Staff | StageActor.Specialist,
            ActionRequiredMessage = "Review the proposal and accept or decline.",
            WaitingMessage = "Waiting for client to review and accept the proposal."
        },

        [StudyStatus.ProposalDeclined] = new StageConfig
        {
            Status = StudyStatus.ProposalDeclined,
            DisplayName = "Proposal Declined",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.ProposalCreated, // Can create revision
            AlternativeTransitions = new() { 
                [StageAction.Cancel] = StudyStatus.RequestCancelled 
            },
            NotifyOnEnter = StageActor.Staff | StageActor.Specialist,
            ActionRequiredMessage = "Review decline feedback and create a revised proposal or cancel the study.",
            WaitingMessage = "The proposal was declined. A revised proposal may be prepared."
        },

        [StudyStatus.ProposalAccepted] = new StageConfig
        {
            Status = StudyStatus.ProposalAccepted,
            DisplayName = "Proposal Accepted",
            Phase = "Proposal",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            AutoAdvance = false,
            DefaultNextStage = StudyStatus.ServiceContactsRequested,
            NotifyOnEnter = StageActor.Staff | StageActor.Specialist,
            ActionRequiredMessage = "Begin data collection phase.",
            WaitingMessage = "Thank you for accepting! We'll begin collecting information shortly."
        },

        // ═══════════════════════════════════════════════════════════════
        // DATA COLLECTION PHASE (Parallel capable)
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.ServiceContactsRequested] = new StageConfig
        {
            Status = StudyStatus.ServiceContactsRequested,
            DisplayName = "Service Contacts Requested",
            Phase = "Data Collection",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ServiceContactsProvided },
            DefaultNextStage = StudyStatus.FinancialInfoRequested,
            ParallelWith = new[] { StudyStatus.FinancialInfoRequested },
            NotifyOnEnter = StageActor.HOA,
            ActionRequiredMessage = "Request service contact information from client.",
            WaitingMessage = "Please provide your service contact information."
        },

        [StudyStatus.FinancialInfoRequested] = new StageConfig
        {
            Status = StudyStatus.FinancialInfoRequested,
            DisplayName = "Financial Info Requested",
            Phase = "Data Collection",
            AdvancedBy = StageActor.System, // Auto-advances when HOA starts form
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.FinancialInfoFormSent },
            AutoAdvance = true,
            DefaultNextStage = StudyStatus.FinancialInfoInProgress,
            ParallelWith = new[] { StudyStatus.ServiceContactsRequested },
            NotifyOnEnter = StageActor.HOA,
            ActionRequiredMessage = "Financial information request sent to client.",
            WaitingMessage = "Please provide your financial information."
        },

        [StudyStatus.FinancialInfoInProgress] = new StageConfig
        {
            Status = StudyStatus.FinancialInfoInProgress,
            DisplayName = "Financial Info In Progress",
            Phase = "Data Collection",
            AdvancedBy = StageActor.HOA, // HOA submits
            AvailableActions = StageAction.Advance,
            RequiredValidations = new[] { StageValidation.FinancialInfoComplete },
            AutoAdvance = true,
            DefaultNextStage = StudyStatus.FinancialInfoSubmitted,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Complete and submit your financial information.",
            WaitingMessage = "Client is entering financial information."
        },

        [StudyStatus.FinancialInfoSubmitted] = new StageConfig
        {
            Status = StudyStatus.FinancialInfoSubmitted,
            DisplayName = "Financial Info Submitted",
            Phase = "Data Collection",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.RequestChanges,
            DefaultNextStage = StudyStatus.FinancialInfoReviewed,
            AlternativeTransitions = new() { [StageAction.RequestChanges] = StudyStatus.FinancialInfoRequested },
            NotifyOnEnter = StageActor.Staff | StageActor.Specialist,
            ActionRequiredMessage = "Review the submitted financial information.",
            WaitingMessage = "Your financial information is being reviewed."
        },

        [StudyStatus.FinancialInfoReviewed] = new StageConfig
        {
            Status = StudyStatus.FinancialInfoReviewed,
            DisplayName = "Financial Info Reviewed",
            Phase = "Data Collection",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.RequestChanges | StageAction.Cancel,
            DefaultNextStage = StudyStatus.FinancialInfoReceived,
            AlternativeTransitions = new() { [StageAction.RequestChanges] = StudyStatus.FinancialInfoRequested },
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Accept financial info or request additional information.",
            WaitingMessage = "Financial information review in progress."
        },

        [StudyStatus.FinancialInfoReceived] = new StageConfig
        {
            Status = StudyStatus.FinancialInfoReceived,
            DisplayName = "Financial Info Complete",
            Phase = "Data Collection",
            AdvancedBy = StageActor.Staff | StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.SiteVisitPending,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Financial information complete. Begin site visit phase.",
            WaitingMessage = "Financial information has been accepted."
        },

        // ═══════════════════════════════════════════════════════════════
        // SITE VISIT PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.SiteVisitPending] = new StageConfig
        {
            Status = StudyStatus.SiteVisitPending,
            DisplayName = "Site Visit Pending",
            Phase = "Site Visit",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.SiteVisitScheduled,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Schedule the site visit with the client.",
            WaitingMessage = "Your specialist will contact you to schedule the site visit."
        },

        [StudyStatus.SiteVisitScheduled] = new StageConfig
        {
            Status = StudyStatus.SiteVisitScheduled,
            DisplayName = "Site Visit Scheduled",
            Phase = "Site Visit",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.SiteVisitDateSelected },
            DefaultNextStage = StudyStatus.SiteVisitCompleted,
            NotifyOnEnter = StageActor.HOA,
            ActionRequiredMessage = "Complete the site visit.",
            WaitingMessage = "Your site visit has been scheduled."
        },

        [StudyStatus.SiteVisitCompleted] = new StageConfig
        {
            Status = StudyStatus.SiteVisitCompleted,
            DisplayName = "Site Visit Completed",
            Phase = "Site Visit",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.SiteVisitPhotosUploaded },
            AutoAdvance = true,
            DefaultNextStage = StudyStatus.SiteVisitDataEntered,
            NotifyOnEnter = StageActor.HOA,
            ActionRequiredMessage = "Upload site visit photos and data.",
            WaitingMessage = "The site visit has been completed."
        },

        [StudyStatus.SiteVisitDataEntered] = new StageConfig
        {
            Status = StudyStatus.SiteVisitDataEntered,
            DisplayName = "Site Visit Data Entered",
            Phase = "Site Visit",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.SiteVisitDataComplete },
            DefaultNextStage = StudyStatus.FundingPlanReady,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Complete data entry and begin funding plan.",
            WaitingMessage = "Site visit data is being processed."
        },

        // ═══════════════════════════════════════════════════════════════
        // FUNDING PLAN PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.FundingPlanReady] = new StageConfig
        {
            Status = StudyStatus.FundingPlanReady,
            DisplayName = "Funding Plan Ready",
            Phase = "Funding Plan",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.FundingPlanInProcess,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Begin creating the funding plan.",
            WaitingMessage = "Your funding plan is being prepared."
        },

        [StudyStatus.FundingPlanInProcess] = new StageConfig
        {
            Status = StudyStatus.FundingPlanInProcess,
            DisplayName = "Funding Plan In Progress",
            Phase = "Funding Plan",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.FundingPlanCreated },
            DefaultNextStage = StudyStatus.FundingPlanComplete,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Complete the funding plan.",
            WaitingMessage = "Your funding plan is being developed."
        },

        [StudyStatus.FundingPlanComplete] = new StageConfig
        {
            Status = StudyStatus.FundingPlanComplete,
            DisplayName = "Funding Plan Complete",
            Phase = "Funding Plan",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.FundingPlanComplete },
            DefaultNextStage = StudyStatus.NarrativeReady,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Funding plan complete. Begin narrative phase.",
            WaitingMessage = "Your funding plan has been completed."
        },

        // ═══════════════════════════════════════════════════════════════
        // NARRATIVE/REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.NarrativeReady] = new StageConfig
        {
            Status = StudyStatus.NarrativeReady,
            DisplayName = "Narrative Ready",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.NarrativeInProcess,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Begin writing the narrative.",
            WaitingMessage = "Your report narrative is being prepared."
        },

        [StudyStatus.NarrativeInProcess] = new StageConfig
        {
            Status = StudyStatus.NarrativeInProcess,
            DisplayName = "Narrative In Progress",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.NarrativeCreated },
            DefaultNextStage = StudyStatus.NarrativeComplete,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Complete the narrative.",
            WaitingMessage = "The narrative is being written."
        },

        [StudyStatus.NarrativeComplete] = new StageConfig
        {
            Status = StudyStatus.NarrativeComplete,
            DisplayName = "Narrative Complete",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.NarrativeComplete },
            DefaultNextStage = StudyStatus.NarrativePrintReady,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Prepare narrative for printing.",
            WaitingMessage = "The narrative has been completed."
        },

        [StudyStatus.NarrativePrintReady] = new StageConfig
        {
            Status = StudyStatus.NarrativePrintReady,
            DisplayName = "Ready for Printing",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.NarrativePackaged,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Print and package the report.",
            WaitingMessage = "Your report is being prepared for delivery."
        },

        [StudyStatus.NarrativePackaged] = new StageConfig
        {
            Status = StudyStatus.NarrativePackaged,
            DisplayName = "Report Packaged",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.NarrativeSent,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Send the narrative to the client.",
            WaitingMessage = "Your report has been packaged."
        },

        [StudyStatus.NarrativeSent] = new StageConfig
        {
            Status = StudyStatus.NarrativeSent,
            DisplayName = "Narrative Sent",
            Phase = "Narrative",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.ReportReady,
            NotifyOnEnter = StageActor.HOA,
            ActionRequiredMessage = "Prepare final report.",
            WaitingMessage = "The narrative has been sent to you."
        },

        // ═══════════════════════════════════════════════════════════════
        // FINAL REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.ReportReady] = new StageConfig
        {
            Status = StudyStatus.ReportReady,
            DisplayName = "Report Ready",
            Phase = "Final Report",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            DefaultNextStage = StudyStatus.ReportInProcess,
            NotifyOnEnter = StageActor.Specialist,
            ActionRequiredMessage = "Finalize the report.",
            WaitingMessage = "The final report is being prepared."
        },

        [StudyStatus.ReportInProcess] = new StageConfig
        {
            Status = StudyStatus.ReportInProcess,
            DisplayName = "Report In Progress",
            Phase = "Final Report",
            AdvancedBy = StageActor.Specialist,
            AvailableActions = StageAction.Advance | StageAction.Cancel,
            RequiredValidations = new[] { StageValidation.ReportAttached },
            DefaultNextStage = StudyStatus.ReportComplete,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "Complete and attach the final report.",
            WaitingMessage = "The final report is being finalized."
        },

        [StudyStatus.ReportComplete] = new StageConfig
        {
            Status = StudyStatus.ReportComplete,
            DisplayName = "Report Complete",
            Phase = "Final Report",
            AdvancedBy = StageActor.Staff | StageActor.Admin,
            AvailableActions = StageAction.Advance,
            RequiredValidations = new[] { StageValidation.ReportApproved },
            DefaultNextStage = StudyStatus.RequestCompleted,
            NotifyOnEnter = StageActor.Admin | StageActor.HOA,
            ActionRequiredMessage = "Approve and complete the study.",
            WaitingMessage = "Your reserve study report is complete!"
        },

        [StudyStatus.FinalReviewPending] = new StageConfig
        {
            Status = StudyStatus.FinalReviewPending,
            DisplayName = "Pending Final Review",
            Phase = "Final Report",
            AdvancedBy = StageActor.Staff | StageActor.Admin,
            AvailableActions = StageAction.Advance,
            RequiredValidations = Array.Empty<StageValidation>(),
            DefaultNextStage = StudyStatus.RequestCompleted,
            NotifyOnEnter = StageActor.Staff | StageActor.Admin,
            ActionRequiredMessage = "Complete final review before marking study complete.",
            WaitingMessage = "Awaiting final review by tenant owner."
        },

        // ═══════════════════════════════════════════════════════════════
        // COMPLETION
        // ═══════════════════════════════════════════════════════════════
        [StudyStatus.RequestCompleted] = new StageConfig
        {
            Status = StudyStatus.RequestCompleted,
            DisplayName = "Study Completed",
            Phase = "Completion",
            AdvancedBy = StageActor.Admin | StageActor.System,
            AvailableActions = StageAction.Advance, // Archive
            DefaultNextStage = StudyStatus.RequestArchived,
            NotifyOnEnter = StageActor.HOA | StageActor.Staff,
            ActionRequiredMessage = "Archive the completed study.",
            WaitingMessage = "Your reserve study has been completed!"
        },

        [StudyStatus.RequestCancelled] = new StageConfig
        {
            Status = StudyStatus.RequestCancelled,
            DisplayName = "Cancelled",
            Phase = "Completion",
            AdvancedBy = StageActor.Admin, // Only admin can reverse
            AvailableActions = StageAction.None,
            DefaultNextStage = null,
            NotifyOnEnter = StageActor.HOA | StageActor.Staff,
            ActionRequiredMessage = "",
            WaitingMessage = "This request has been cancelled."
        },

        [StudyStatus.RequestArchived] = new StageConfig
        {
            Status = StudyStatus.RequestArchived,
            DisplayName = "Archived",
            Phase = "Completion",
            AdvancedBy = StageActor.None, // Terminal
            AvailableActions = StageAction.None,
            DefaultNextStage = null,
            NotifyOnEnter = StageActor.None,
            ActionRequiredMessage = "",
            WaitingMessage = "This study has been archived."
        }
    };

    /// <summary>
    /// Gets the configuration for a specific stage.
    /// </summary>
    public static StageConfig GetConfig(StudyStatus status)
    {
        return _configs.TryGetValue(status, out var config) ? config : new StageConfig
        {
            Status = status,
            DisplayName = status.ToString(),
            Phase = "Unknown"
        };
    }

    /// <summary>
    /// Gets all stage configurations.
    /// </summary>
    public static IReadOnlyDictionary<StudyStatus, StageConfig> All => _configs;

    /// <summary>
    /// Checks if an actor can perform an action on the current stage.
    /// </summary>
    public static bool CanActorPerformAction(StudyStatus status, StageActor actor, StageAction action)
    {
        var config = GetConfig(status);
        
        // Admin can always perform admin actions
        if (actor.HasFlag(StageActor.Admin))
        {
            var adminActions = StageAction.Skip | StageAction.MoveBack | StageAction.ForceComplete;
            if ((action & adminActions) != 0) return true;
        }
        
        // Check if the action is available for this stage
        if (!config.AvailableActions.HasFlag(action)) return false;
        
        // Check if this actor can advance
        if (action == StageAction.Advance && !config.AdvancedBy.HasFlag(actor)) return false;
        
        return true;
    }

    /// <summary>
    /// Gets the next stage for a given action.
    /// </summary>
    public static StudyStatus? GetNextStage(StudyStatus currentStatus, StageAction action)
    {
        var config = GetConfig(currentStatus);
        
        if (action == StageAction.Advance)
            return config.DefaultNextStage;
            
        if (config.AlternativeTransitions.TryGetValue(action, out var altStage))
            return altStage;
            
        return null;
    }
}
