using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.EventsAndListeners
{
    /// <summary>
    /// Handles ProposalDeclinedEvent by sending notification emails to specialists
    /// </summary>
    public class ProposalDeclinedListener : IListener<ProposalDeclinedEvent>
    {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<ProposalDeclinedListener> _logger;

        public ProposalDeclinedListener(
            IMailer mailer,
            IReserveStudyService reserveStudyService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<ProposalDeclinedListener> logger)
        {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(ProposalDeclinedEvent broadcasted)
        {
            var study = broadcasted.ReserveStudy;
            var proposal = broadcasted.Proposal;

            // Reload study with all needed navigation properties
            await using var db = await _dbFactory.CreateDbContextAsync();
            study = await db.ReserveStudies
                .Include(s => s.User)
                .Include(s => s.Community)
                .Include(s => s.Contact)
                .Include(s => s.PropertyManager)
                .Include(s => s.CurrentProposal)
                .FirstOrDefaultAsync(s => s.Id == study.Id);

            if (study == null)
            {
                _logger.LogWarning("Could not find study {StudyId} for proposal declined notification", broadcasted.ReserveStudy.Id);
                return;
            }

            // If CurrentProposal wasn't loaded, use the proposal from the event
            if (study.CurrentProposal == null && proposal != null)
            {
                study.CurrentProposal = proposal;
            }

            // Build the notification message
            var reasonText = GetReasonDisplayText(proposal.DeclineReasonCategory);
            var revisionText = broadcasted.RevisionRequested 
                ? "The customer has requested a revised proposal." 
                : "The customer has not requested a revision.";

            var message = $@"A proposal has been declined for the reserve study for {study.Community?.Name}.

**Reason:** {reasonText}
{(string.IsNullOrWhiteSpace(proposal.DeclineComments) ? "" : $"\n**Customer Comments:** {proposal.DeclineComments}")}

**Declined By:** {proposal.DeclinedBy}
**Declined On:** {proposal.DateDeclined?.ToLocalTime():MMMM dd, yyyy 'at' h:mm tt}

{revisionText}";

            var email = await _reserveStudyService.MapToReserveStudyEmailAsync(study, message);

            // Build subject
            var companyName = email.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subjectPrefix = broadcasted.RevisionRequested ? "Revision Requested" : "Proposal Declined";
            var subject = $"[{companyName}] {subjectPrefix}: {study.Community?.Name}";

            // Get recipient from tenant's default notification email or fallback to tenant branding email
            var recipientEmail = email.TenantInfo?.DefaultNotificationEmail ?? email.TenantInfo?.FromEmail;

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogWarning("No recipient email configured for tenant {TenantId} for proposal declined notification", study.TenantId);
                return;
            }

            _logger.LogInformation(
                "Sending proposal declined notification to {Email} for study {StudyId}. Revision requested: {RevisionRequested}",
                recipientEmail, study.Id, broadcasted.RevisionRequested);

            try
            {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipientEmail)
                        .Subject(subject)
                        .ReplyTo(study.PointOfContact?.Email ?? study.User?.Email ?? "")
                        .View("~/Components/EmailTemplates/ReserveStudyProposalDeclined.cshtml", email)
                );

                _logger.LogInformation("Proposal declined notification sent successfully for study {StudyId}", study.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send proposal declined notification for study {StudyId}", study.Id);
                throw;
            }
        }

        private static string GetReasonDisplayText(ProposalDeclineReason? reason)
        {
            return reason switch
            {
                ProposalDeclineReason.PriceTooHigh => "Price is above budget",
                ProposalDeclineReason.ScopeInadequate => "Scope doesn't meet requirements",
                ProposalDeclineReason.TimelineUnacceptable => "Timeline doesn't work",
                ProposalDeclineReason.PaymentTermsUnacceptable => "Payment terms not acceptable",
                ProposalDeclineReason.ChoseCompetitor => "Chose a different company",
                ProposalDeclineReason.ProjectCancelled => "Project cancelled or no longer needed",
                ProposalDeclineReason.BudgetConstraints => "Budget constraints / funding issues",
                ProposalDeclineReason.Other => "Other reason (see comments)",
                _ => "Not specified"
            };
        }
    }
}
