

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

namespace CRS.EventsAndListeners {
    public class ReserveStudyCreatedListener : IListener<ReserveStudyCreatedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ICalendarService _calendarService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<ReserveStudyCreatedListener> _logger;

        public ReserveStudyCreatedListener(
            IMailer mailer, 
            IReserveStudyService reserveStudyService, 
            ICalendarService calendarService,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<ReserveStudyCreatedListener> logger) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _calendarService = calendarService;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task HandleAsync(ReserveStudyCreatedEvent broadcasted) {
            var reserveStudy = broadcasted.ReserveStudy;

            // Reload study with all needed navigation properties
            await using var db = await _dbFactory.CreateDbContextAsync();
            reserveStudy = await db.ReserveStudies
                .Include(s => s.User)
                .Include(s => s.Specialist)
                .Include(s => s.Community)
                    .ThenInclude(c => c!.PhysicalAddress)
                .Include(s => s.Contact)
                .Include(s => s.PropertyManager)
                .FirstOrDefaultAsync(s => s.Id == reserveStudy.Id);

            if (reserveStudy == null)
            {
                _logger.LogWarning("Could not find study {StudyId} for reserve study created email", broadcasted.ReserveStudy.Id);
                return;
            }

            #region Add Annual Meeting to Calendar
            if (reserveStudy.Community?.AnnualMeetingDate != null) {
                var annualMeetingDate = reserveStudy.Community.AnnualMeetingDate.Value;
                var calendarEvent = new CalendarEvent {
                    Title = "Annual Meeting",
                    Start = annualMeetingDate,
                    End = annualMeetingDate.AddHours(2),
                    Description = "Annual meeting for the community.",
                    Location = reserveStudy.Community.PhysicalAddress?.FullAddress ?? string.Empty,
                    ApplicationUserId = reserveStudy.ApplicationUserId,
                    SpecialistUserId = reserveStudy.SpecialistUserId,
                    IsPublic = true
                };

                await _calendarService.AddEventAsync(calendarEvent);
            }
            #endregion

            #region Send Email
            var reserveStudyEmail = await _reserveStudyService.MapToReserveStudyEmailAsync(reserveStudy, 
                "A reserve study request has been shared with you. You can view the details online or contact the assigned specialist for more information.");

            var companyName = reserveStudyEmail.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Reserve Study Request for {reserveStudy.Community?.Name}";

            var emailRecipients = new List<string>();

            if (!string.IsNullOrEmpty(reserveStudy.Specialist?.Email)) {
                emailRecipients.Add(reserveStudy.Specialist.Email);
            }

            if (!string.IsNullOrEmpty(reserveStudy.PointOfContact?.Email)) {
                emailRecipients.Add(reserveStudy.PointOfContact.Email);
            }

            if (emailRecipients.Count == 0 && !string.IsNullOrEmpty(reserveStudyEmail.TenantInfo?.DefaultNotificationEmail)) {
                emailRecipients.Add(reserveStudyEmail.TenantInfo.DefaultNotificationEmail);
            }

            if (emailRecipients.Count == 0)
            {
                _logger.LogWarning("No recipients found for reserve study created email. Study ID: {StudyId}", reserveStudy.Id);
                return;
            }

            try {
                await _mailer.SendAsync(
                    Mailable.AsInline<ReserveStudyEmail>()
                        .To(emailRecipients.ToArray())
                        .Subject(subject)
                        .ReplyToTenant(reserveStudyEmail.TenantInfo)
                        .View("~/Components/EmailTemplates/ReserveStudyCreate.cshtml", reserveStudyEmail)
                );
                _logger.LogInformation("Reserve study created email sent to {Recipients} for study {StudyId}", 
                    string.Join(", ", emailRecipients), reserveStudy.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to send reserve study created email for study {StudyId}", reserveStudy.Id);
                throw;
            }
            #endregion
        }
    }
}
