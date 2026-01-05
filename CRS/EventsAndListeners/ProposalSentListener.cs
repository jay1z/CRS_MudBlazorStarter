using System.Threading.Tasks;
using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRS.EventsAndListeners
{
    public class ProposalSentListener : IListener<ProposalSentEvent>
    {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ILogger<ProposalSentListener> _logger;

        public ProposalSentListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService,
            ILogger<ProposalSentListener> logger)
        {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _logger = logger;
        }

        public async Task HandleAsync(ProposalSentEvent broadcasted)
        {
            var study = broadcasted.ReserveStudy;
            var proposal = broadcasted.Proposal;
            
            var subject = $"Reserve Study Proposal for {study.Community?.Name}";
            var message = $"A proposal has been prepared for your reserve study. Please review the proposal details and accept it to proceed.";
            
            var recipients = new List<string>();
            
            // Primary recipient: The HOA user who owns this study
            if (study.User?.Email != null)
            {
                recipients.Add(study.User.Email);
                _logger.LogInformation("Adding study owner email: {Email}", study.User.Email);
            }
            
            // Also send to the point of contact if different from the user
            if (study.PointOfContact?.Email != null && 
                !recipients.Contains(study.PointOfContact.Email, StringComparer.OrdinalIgnoreCase))
            {
                recipients.Add(study.PointOfContact.Email);
                _logger.LogInformation("Adding point of contact email: {Email}", study.PointOfContact.Email);
            }
            
            // If no recipients found, log a warning
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for proposal sent email. Study ID: {StudyId}", study.Id);
                return;
            }
            
            _logger.LogInformation("Sending proposal email to {Count} recipient(s) for study {StudyId}", 
                recipients.Count, study.Id);
            
            var email = _reserveStudyService.MapToReserveStudyEmail(study, message);
            
            try
            {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(recipients.ToArray())
                        .Subject(subject)
                        .View("~/Components/EmailTemplates/ReserveStudyProposalSent.cshtml", email)
                );
                
                _logger.LogInformation("Proposal email sent successfully for study {StudyId}", study.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send proposal email for study {StudyId}", study.Id);
                throw;
            }
        }
    }
}