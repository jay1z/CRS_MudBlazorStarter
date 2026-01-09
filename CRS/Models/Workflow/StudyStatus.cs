using System;

namespace CRS.Models.Workflow {
    /// <summary>
    /// Represents the lifecycle states for a Reserve Study request from submission to archival.
    /// Matches the 32-stage workflow document.
    /// </summary>
    public enum StudyStatus {
        // ═══════════════════════════════════════════════════════════════
        // REQUEST PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Initial request submitted by HOA/client.
        /// Transitions: → RequestApproved | RequestCancelled
        /// </summary>
        RequestCreated = 0,

        /// <summary>
        /// Request has been reviewed and approved for proposal creation.
        /// Transitions: → ProposalCreated
        /// </summary>
        RequestApproved = 1,

        // ═══════════════════════════════════════════════════════════════
        // PROPOSAL PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Staff creates proposal for the study.
        /// Transitions: → ProposalReviewed
        /// </summary>
        ProposalCreated = 2,

        /// <summary>
        /// Internal review of proposal.
        /// Transitions: → ProposalUpdated | ProposalApproved
        /// </summary>
        ProposalReviewed = 3,

        /// <summary>
        /// Proposal modified after review.
        /// Transitions: → ProposalReviewed | ProposalApproved
        /// </summary>
        ProposalUpdated = 4,

        /// <summary>
        /// Internal approval of proposal.
        /// Transitions: → ProposalSent
        /// </summary>
        ProposalApproved = 5,

        /// <summary>
        /// Proposal sent to client.
        /// Transitions: → ProposalAccepted | RequestCancelled
        /// </summary>
        ProposalSent = 6,

        /// <summary>
        /// Client accepts the proposal.
        /// Transitions: → ServiceContactsRequested | FinancialInfoRequested
        /// </summary>
        ProposalAccepted = 7,

        // ═══════════════════════════════════════════════════════════════
        // DATA COLLECTION PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Request for service contacts sent.
        /// Transitions: → FinancialInfoRequested
        /// </summary>
        ServiceContactsRequested = 8,

        /// <summary>
        /// Request for financial data sent to client.
        /// Transitions: → FinancialInfoInProgress
        /// </summary>
        FinancialInfoRequested = 9,

        /// <summary>
        /// Client is entering financial info (form in progress).
        /// Transitions: → FinancialInfoSubmitted
        /// </summary>
        FinancialInfoInProgress = 10,

        /// <summary>
        /// Client submits financial info.
        /// Transitions: → FinancialInfoReviewed
        /// </summary>
        FinancialInfoSubmitted = 11,

        /// <summary>
        /// Staff reviews financial info.
        /// Transitions: → FinancialInfoReceived | FinancialInfoRequested
        /// </summary>
        FinancialInfoReviewed = 12,

        /// <summary>
        /// Financial info accepted/complete.
        /// Transitions: → SiteVisitPending
        /// </summary>
        FinancialInfoReceived = 13,

        // ═══════════════════════════════════════════════════════════════
        // SITE VISIT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Ready to schedule site visit.
        /// Transitions: → SiteVisitScheduled
        /// </summary>
        SiteVisitPending = 14,

        /// <summary>
        /// Site visit date scheduled.
        /// Transitions: → SiteVisitCompleted
        /// </summary>
        SiteVisitScheduled = 15,

        /// <summary>
        /// Physical site visit completed.
        /// Transitions: → SiteVisitDataEntered
        /// </summary>
        SiteVisitCompleted = 16,

        /// <summary>
        /// Site visit data entered into system.
        /// Transitions: → FundingPlanReady | AmendmentPending (if scope variance exceeds threshold)
        /// </summary>
        SiteVisitDataEntered = 17,

        // ═══════════════════════════════════════════════════════════════
        // AMENDMENT PHASE (triggered by scope variance after site visit)
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Scope variance exceeds threshold - amendment sent to HOA for approval.
        /// This is a blocking state that requires HOA action before proceeding.
        /// Transitions: → FundingPlanReady (if accepted) | RequestCancelled (if rejected and no resolution)
        /// </summary>
        AmendmentPending = 50,

        // ═══════════════════════════════════════════════════════════════
        // FUNDING PLAN PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Ready to create funding plan.
        /// Transitions: → FundingPlanInProcess
        /// </summary>
        FundingPlanReady = 18,

        /// <summary>
        /// Funding plan being developed.
        /// Transitions: → FundingPlanComplete
        /// </summary>
        FundingPlanInProcess = 19,

        /// <summary>
        /// Funding plan finished.
        /// Transitions: → NarrativeReady
        /// </summary>
        FundingPlanComplete = 20,

        // ═══════════════════════════════════════════════════════════════
        // NARRATIVE/REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Ready to write narrative.
        /// Transitions: → NarrativeInProcess
        /// </summary>
        NarrativeReady = 21,

        /// <summary>
        /// Narrative being written.
        /// Transitions: → NarrativeComplete
        /// </summary>
        NarrativeInProcess = 22,

        /// <summary>
        /// Narrative finished.
        /// Transitions: → NarrativePrintReady
        /// </summary>
        NarrativeComplete = 23,

        /// <summary>
        /// Ready for printing.
        /// Transitions: → NarrativePackaged
        /// </summary>
        NarrativePrintReady = 24,

        /// <summary>
        /// Report packaged for delivery.
        /// Transitions: → NarrativeSent
        /// </summary>
        NarrativePackaged = 25,

        /// <summary>
        /// Narrative sent to client.
        /// Transitions: → ReportReady
        /// </summary>
        NarrativeSent = 26,

        // ═══════════════════════════════════════════════════════════════
        // FINAL REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Final report ready.
        /// Transitions: → ReportInProcess
        /// </summary>
        ReportReady = 27,

        /// <summary>
        /// Final report being finalized.
        /// Transitions: → ReportComplete
        /// </summary>
        ReportInProcess = 28,

        /// <summary>
        /// Report finalized and delivered.
        /// Transitions: → RequestCompleted
        /// </summary>
        ReportComplete = 29,

        // ═══════════════════════════════════════════════════════════════
        // TERMINAL STATES
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Study completed successfully.
        /// Terminal state.
        /// </summary>
        RequestCompleted = 30,

        /// <summary>
        /// Request cancelled before completion.
        /// Terminal state.
        /// </summary>
        RequestCancelled = 31,

        /// <summary>
        /// Completed study archived for records.
        /// Terminal state.
        /// </summary>
        RequestArchived = 32
    }
}
