using CRS.Models.Workflow;

namespace CRS.Services.Workflow;

/// <summary>
/// Extension methods for StudyStatus display formatting.
/// </summary>
public static class StudyStatusExtensions
{
    /// <summary>
    /// Gets a display-friendly title for a StudyStatus.
    /// </summary>
    public static string ToDisplayTitle(this StudyStatus status) => status switch
    {
        // Request Phase
        StudyStatus.RequestCreated => "Request Created",
        StudyStatus.RequestApproved => "Request Approved",
        
        // Proposal Phase
        StudyStatus.ProposalCreated => "Proposal Created",
        StudyStatus.ProposalReviewed => "Proposal Under Review",
        StudyStatus.ProposalUpdated => "Proposal Updated",
        StudyStatus.ProposalApproved => "Proposal Approved",
        StudyStatus.ProposalSent => "Proposal Sent",
        StudyStatus.ProposalAccepted => "Proposal Accepted",
        
        // Data Collection Phase
        StudyStatus.ServiceContactsRequested => "Service Contacts Requested",
        StudyStatus.FinancialInfoRequested => "Financial Info Requested",
        StudyStatus.FinancialInfoInProgress => "Financial Info In Progress",
        StudyStatus.FinancialInfoSubmitted => "Financial Info Submitted",
        StudyStatus.FinancialInfoReviewed => "Financial Info Reviewed",
        StudyStatus.FinancialInfoReceived => "Financial Info Complete",
        
        // Site Visit Phase
        StudyStatus.SiteVisitPending => "Site Visit Pending",
        StudyStatus.SiteVisitScheduled => "Site Visit Scheduled",
        StudyStatus.SiteVisitCompleted => "Site Visit Completed",
        StudyStatus.SiteVisitDataEntered => "Site Visit Data Entered",
        
        // Funding Plan Phase
        StudyStatus.FundingPlanReady => "Funding Plan Ready",
        StudyStatus.FundingPlanInProcess => "Funding Plan In Progress",
        StudyStatus.FundingPlanComplete => "Funding Plan Complete",
        
        // Narrative/Report Phase
        StudyStatus.NarrativeReady => "Narrative Ready",
        StudyStatus.NarrativeInProcess => "Narrative In Progress",
        StudyStatus.NarrativeComplete => "Narrative Complete",
        StudyStatus.NarrativePrintReady => "Ready for Printing",
        StudyStatus.NarrativePackaged => "Report Packaged",
        StudyStatus.NarrativeSent => "Narrative Sent",
        
        // Final Report Phase
        StudyStatus.ReportReady => "Report Ready",
        StudyStatus.ReportInProcess => "Report In Progress",
        StudyStatus.ReportComplete => "Report Complete",
        
        // Completion
        StudyStatus.RequestCompleted => "Study Completed",
        StudyStatus.RequestCancelled => "Cancelled",
        StudyStatus.RequestArchived => "Archived",
        
        _ => status.ToString()
    };

    /// <summary>
    /// Gets a description of the current step for a StudyStatus.
    /// </summary>
    public static string ToStepDescription(this StudyStatus status) => status switch
    {
        // Request Phase
        StudyStatus.RequestCreated => 
            "Your reserve study request has been received and is being reviewed by our team.",
        StudyStatus.RequestApproved => 
            "Your request has been approved. A specialist will be assigned shortly.",
        
        // Proposal Phase
        StudyStatus.ProposalCreated => 
            "A proposal is being created for your reserve study.",
        StudyStatus.ProposalReviewed => 
            "The proposal is under internal review.",
        StudyStatus.ProposalUpdated => 
            "The proposal has been updated based on review feedback.",
        StudyStatus.ProposalApproved => 
            "The proposal has been internally approved and will be sent to you shortly.",
        StudyStatus.ProposalSent => 
            "A proposal has been sent. Please review and let us know if you'd like to proceed.",
        StudyStatus.ProposalAccepted => 
            "Thank you for accepting the proposal! We'll begin collecting the required information.",
        
        // Data Collection Phase
        StudyStatus.ServiceContactsRequested => 
            "We've requested your service contact information.",
        StudyStatus.FinancialInfoRequested => 
            "We've requested financial information about your reserve fund.",
        StudyStatus.FinancialInfoInProgress => 
            "Financial information form is in progress.",
        StudyStatus.FinancialInfoSubmitted => 
            "Your financial information has been submitted and is awaiting review.",
        StudyStatus.FinancialInfoReviewed => 
            "Your financial information is being reviewed by our team.",
        StudyStatus.FinancialInfoReceived => 
            "Financial information has been accepted. Preparing for site visit.",
        
        // Site Visit Phase
        StudyStatus.SiteVisitPending => 
            "Ready to schedule your site visit.",
        StudyStatus.SiteVisitScheduled => 
            "Your site visit has been scheduled.",
        StudyStatus.SiteVisitCompleted => 
            "The site visit has been completed.",
        StudyStatus.SiteVisitDataEntered => 
            "Site visit data has been entered into the system.",
        
        // Funding Plan Phase
        StudyStatus.FundingPlanReady => 
            "Ready to create your funding plan.",
        StudyStatus.FundingPlanInProcess => 
            "Your funding plan is being developed.",
        StudyStatus.FundingPlanComplete => 
            "Your funding plan has been completed.",
        
        // Narrative/Report Phase
        StudyStatus.NarrativeReady => 
            "Ready to write the reserve study narrative.",
        StudyStatus.NarrativeInProcess => 
            "The narrative is being written.",
        StudyStatus.NarrativeComplete => 
            "The narrative has been completed.",
        StudyStatus.NarrativePrintReady => 
            "The report is ready for printing.",
        StudyStatus.NarrativePackaged => 
            "The report has been packaged for delivery.",
        StudyStatus.NarrativeSent => 
            "The narrative has been sent to you.",
        
        // Final Report Phase
        StudyStatus.ReportReady => 
            "The final report is ready.",
        StudyStatus.ReportInProcess => 
            "The final report is being finalized.",
        StudyStatus.ReportComplete => 
            "Your reserve study report has been completed and is ready for review.",
        
        // Completion
        StudyStatus.RequestCompleted => 
            "Your reserve study has been successfully completed.",
        StudyStatus.RequestCancelled => 
            "This reserve study request has been cancelled.",
        StudyStatus.RequestArchived => 
            "This reserve study has been archived.",
        
        _ => "Your reserve study is in progress."
    };

    /// <summary>
    /// Gets a description of the next step for a StudyStatus.
    /// </summary>
    public static string ToNextStepDescription(this StudyStatus status) => status switch
    {
        StudyStatus.RequestCreated => 
            "Our team will create a proposal outlining the scope and costs.",
        StudyStatus.RequestApproved => 
            "We'll request your service contacts and financial information.",
        StudyStatus.ProposalCreated => 
            "The proposal will be reviewed internally.",
        StudyStatus.ProposalReviewed => 
            "The proposal will be approved or updated based on feedback.",
        StudyStatus.ProposalUpdated => 
            "The updated proposal will be reviewed again.",
        StudyStatus.ProposalApproved => 
            "The proposal will be sent to you for review.",
        StudyStatus.ProposalSent => 
            "Please review and accept the proposal to proceed.",
        StudyStatus.ProposalAccepted => 
            "We'll request your service contacts and financial information.",
        StudyStatus.ServiceContactsRequested => 
            "We'll request your financial information.",
        StudyStatus.FinancialInfoRequested => 
            "Please provide the requested financial information.",
        StudyStatus.FinancialInfoInProgress => 
            "Please complete and submit your financial information.",
        StudyStatus.FinancialInfoSubmitted => 
            "Our team will review your financial information.",
        StudyStatus.FinancialInfoReviewed => 
            "Once approved, we'll prepare for the site visit.",
        StudyStatus.FinancialInfoReceived => 
            "We'll begin the site visit phase.",
        StudyStatus.SiteVisitPending => 
            "We'll schedule your site visit.",
        StudyStatus.SiteVisitScheduled => 
            "The specialist will conduct the on-site inspection.",
        StudyStatus.SiteVisitCompleted => 
            "Site visit data will be entered into the system.",
        StudyStatus.SiteVisitDataEntered => 
            "We'll begin creating your funding plan.",
        StudyStatus.FundingPlanReady => 
            "Your funding plan will be developed.",
        StudyStatus.FundingPlanInProcess => 
            "The funding plan will be completed.",
        StudyStatus.FundingPlanComplete => 
            "We'll begin writing the narrative.",
        StudyStatus.NarrativeReady => 
            "The narrative will be written.",
        StudyStatus.NarrativeInProcess => 
            "The narrative will be completed.",
        StudyStatus.NarrativeComplete => 
            "The report will be prepared for printing.",
        StudyStatus.NarrativePrintReady => 
            "The report will be packaged for delivery.",
        StudyStatus.NarrativePackaged => 
            "The narrative will be sent to you.",
        StudyStatus.NarrativeSent => 
            "The final report will be prepared.",
        StudyStatus.ReportReady => 
            "The final report will be finalized.",
        StudyStatus.ReportInProcess => 
            "The report will be completed and ready for delivery.",
        StudyStatus.ReportComplete => 
            "The study will be marked as completed.",
        StudyStatus.RequestCompleted => 
            "The study will be archived for records.",
        _ => "The next action will be determined based on the current status."
    };

    /// <summary>
    /// Gets the action text for what needs to happen next.
    /// </summary>
    public static string ToNextActionText(this StudyStatus status) => status switch
    {
        StudyStatus.RequestCreated => "Create Proposal",
        StudyStatus.RequestApproved => "Request Information",
        StudyStatus.ProposalCreated => "Review Proposal",
        StudyStatus.ProposalReviewed => "Approve Proposal",
        StudyStatus.ProposalUpdated => "Re-review Proposal",
        StudyStatus.ProposalApproved => "Send Proposal",
        StudyStatus.ProposalSent => "Awaiting Client Response",
        StudyStatus.ProposalAccepted => "Request Information",
        StudyStatus.ServiceContactsRequested => "Request Financial Info",
        StudyStatus.FinancialInfoRequested => "Awaiting Client Input",
        StudyStatus.FinancialInfoInProgress => "Awaiting Submission",
        StudyStatus.FinancialInfoSubmitted => "Review Financial Info",
        StudyStatus.FinancialInfoReviewed => "Accept Financial Info",
        StudyStatus.FinancialInfoReceived => "Begin Site Visit",
        StudyStatus.SiteVisitPending => "Schedule Visit",
        StudyStatus.SiteVisitScheduled => "Complete Visit",
        StudyStatus.SiteVisitCompleted => "Enter Data",
        StudyStatus.SiteVisitDataEntered => "Start Funding Plan",
        StudyStatus.FundingPlanReady => "Develop Plan",
        StudyStatus.FundingPlanInProcess => "Complete Plan",
        StudyStatus.FundingPlanComplete => "Start Narrative",
        StudyStatus.NarrativeReady => "Write Narrative",
        StudyStatus.NarrativeInProcess => "Complete Narrative",
        StudyStatus.NarrativeComplete => "Prepare for Print",
        StudyStatus.NarrativePrintReady => "Package Report",
        StudyStatus.NarrativePackaged => "Send Narrative",
        StudyStatus.NarrativeSent => "Prepare Final Report",
        StudyStatus.ReportReady => "Finalize Report",
        StudyStatus.ReportInProcess => "Complete Report",
        StudyStatus.ReportComplete => "Complete Study",
        StudyStatus.RequestCompleted => "Archive Study",
        _ => status.ToDisplayTitle()
    };

    /// <summary>
    /// Gets the phase name for a status.
    /// </summary>
    public static string ToPhase(this StudyStatus status) => status switch
    {
        StudyStatus.RequestCreated or StudyStatus.RequestApproved => "Request",
        
        StudyStatus.ProposalCreated or StudyStatus.ProposalReviewed or StudyStatus.ProposalUpdated or 
        StudyStatus.ProposalApproved or StudyStatus.ProposalSent or StudyStatus.ProposalAccepted => "Proposal",
        
        StudyStatus.ServiceContactsRequested or StudyStatus.FinancialInfoRequested or StudyStatus.FinancialInfoInProgress or
        StudyStatus.FinancialInfoSubmitted or StudyStatus.FinancialInfoReviewed or StudyStatus.FinancialInfoReceived => "Data Collection",
        
        StudyStatus.SiteVisitPending or StudyStatus.SiteVisitScheduled or 
        StudyStatus.SiteVisitCompleted or StudyStatus.SiteVisitDataEntered => "Site Visit",
        
        StudyStatus.FundingPlanReady or StudyStatus.FundingPlanInProcess or StudyStatus.FundingPlanComplete => "Funding Plan",
        
        StudyStatus.NarrativeReady or StudyStatus.NarrativeInProcess or StudyStatus.NarrativeComplete or
        StudyStatus.NarrativePrintReady or StudyStatus.NarrativePackaged or StudyStatus.NarrativeSent => "Narrative",
        
        StudyStatus.ReportReady or StudyStatus.ReportInProcess or StudyStatus.ReportComplete => "Final Report",
        
        StudyStatus.RequestCompleted or StudyStatus.RequestCancelled or StudyStatus.RequestArchived => "Completion",
        
        _ => "Unknown"
    };
}
