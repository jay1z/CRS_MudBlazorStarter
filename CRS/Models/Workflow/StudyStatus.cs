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
        /// Transitions: → ProposalCreated
        /// </summary>
        RequestCreated = 0,

        // ═══════════════════════════════════════════════════════════════
        // PROPOSAL PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Staff creates proposal for the study.
        /// Transitions: → ProposalReviewed
        /// </summary>
        ProposalCreated = 1,

        /// <summary>
        /// Internal review of proposal.
        /// Transitions: → ProposalUpdated | ProposalApproved
        /// </summary>
        ProposalReviewed = 2,

        /// <summary>
        /// Proposal modified after review.
        /// Transitions: → ProposalReviewed | ProposalApproved
        /// </summary>
        ProposalUpdated = 3,

        /// <summary>
        /// Internal approval of proposal.
        /// Transitions: → ProposalSent
        /// </summary>
        ProposalApproved = 4,

        /// <summary>
        /// Proposal sent to client.
        /// Transitions: → ProposalAccepted | RequestCancelled
        /// </summary>
        ProposalSent = 5,

        /// <summary>
        /// Client accepts the proposal.
        /// Transitions: → ServiceContactsRequested | FinancialInfoRequested
        /// </summary>
        ProposalAccepted = 6,

        // ═══════════════════════════════════════════════════════════════
        // DATA COLLECTION PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Request for service contacts sent.
        /// Transitions: → FinancialInfoRequested
        /// </summary>
        ServiceContactsRequested = 7,

        /// <summary>
        /// Request for financial data sent to client.
        /// Transitions: → FinancialInfoCreated
        /// </summary>
        FinancialInfoRequested = 8,

        /// <summary>
        /// Client starts entering financial info.
        /// Transitions: → FinancialInfoSubmitted
        /// </summary>
        FinancialInfoCreated = 9,

        /// <summary>
        /// Client submits financial info.
        /// Transitions: → FinancialInfoReviewed
        /// </summary>
        FinancialInfoSubmitted = 10,

        /// <summary>
        /// Staff reviews financial info.
        /// Transitions: → FinancialInfoReceived | FinancialInfoRequested
        /// </summary>
        FinancialInfoReviewed = 11,

        /// <summary>
        /// Financial info accepted/complete.
        /// Transitions: → SiteVisit
        /// </summary>
        FinancialInfoReceived = 12,

        // ═══════════════════════════════════════════════════════════════
        // SITE VISIT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Site visit phase begins.
        /// Transitions: → SiteVisitScheduled
        /// </summary>
        SiteVisit = 13,

        /// <summary>
        /// Site visit date scheduled.
        /// Transitions: → SiteVisitCompleted
        /// </summary>
        SiteVisitScheduled = 14,

        /// <summary>
        /// Physical site visit completed.
        /// Transitions: → SiteVisitDataEntered
        /// </summary>
        SiteVisitCompleted = 15,

        /// <summary>
        /// Site visit data entered into system.
        /// Transitions: → FundingPlanReady
        /// </summary>
        SiteVisitDataEntered = 16,

        // ═══════════════════════════════════════════════════════════════
        // FUNDING PLAN PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Ready to create funding plan.
        /// Transitions: → FundingPlanInProcess
        /// </summary>
        FundingPlanReady = 17,

        /// <summary>
        /// Funding plan being developed.
        /// Transitions: → FundingPlanComplete
        /// </summary>
        FundingPlanInProcess = 18,

        /// <summary>
        /// Funding plan finished.
        /// Transitions: → NarrativeReady
        /// </summary>
        FundingPlanComplete = 19,

        // ═══════════════════════════════════════════════════════════════
        // NARRATIVE/REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Ready to write narrative.
        /// Transitions: → NarrativeInProcess
        /// </summary>
        NarrativeReady = 20,

        /// <summary>
        /// Narrative being written.
        /// Transitions: → NarrativeComplete
        /// </summary>
        NarrativeInProcess = 21,

        /// <summary>
        /// Narrative finished.
        /// Transitions: → NarrativePrintReady
        /// </summary>
        NarrativeComplete = 22,

        /// <summary>
        /// Ready for printing.
        /// Transitions: → NarrativePackaged
        /// </summary>
        NarrativePrintReady = 23,

        /// <summary>
        /// Report packaged for delivery.
        /// Transitions: → NarrativeSent
        /// </summary>
        NarrativePackaged = 24,

        /// <summary>
        /// Narrative sent to client.
        /// Transitions: → ReportReady
        /// </summary>
        NarrativeSent = 25,

        // ═══════════════════════════════════════════════════════════════
        // FINAL REPORT PHASE
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Final report ready.
        /// Transitions: → ReportInProcess
        /// </summary>
        ReportReady = 26,

        /// <summary>
        /// Final report being finalized.
        /// Transitions: → ReportComplete
        /// </summary>
        ReportInProcess = 27,

        /// <summary>
        /// Final report complete.
        /// Transitions: → RequestCompleted
        /// </summary>
        ReportComplete = 28,

        // ═══════════════════════════════════════════════════════════════
        // COMPLETION
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Study fully completed and delivered.
        /// Transitions: → RequestArchived
        /// </summary>
        RequestCompleted = 29,

        /// <summary>
        /// Request was cancelled.
        /// Terminal state.
        /// </summary>
        RequestCancelled = 30,

        /// <summary>
        /// Completed study archived.
        /// Terminal state. 3-year renewal reminder scheduled.
        /// </summary>
        RequestArchived = 31
    }
}
