

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Email;
using CRS.Services.Interfaces;

namespace CRS.EventsAndListeners {
    public class ReserveStudyCreatedListener : IListener<ReserveStudyCreatedEvent> {
        private readonly IMailer _mailer;
        private readonly IReserveStudyService _reserveStudyService;
        private readonly ICalendarService _calendarService;

        public ReserveStudyCreatedListener(IMailer mailer, IReserveStudyService reserveStudyService, ICalendarService calendarService) {
            _mailer = mailer;
            _reserveStudyService = reserveStudyService;
            _calendarService = calendarService;
        }

        public async Task HandleAsync(ReserveStudyCreatedEvent broadcasted) {
            var reserveStudy = broadcasted.ReserveStudy;

            #region Add Annual Meeting to Calendar
            if (reserveStudy?.Community?.AnnualMeetingDate != null) {
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

                // Now we can actually add the event to the calendar
                await _calendarService.AddEventAsync(calendarEvent);
            }
            #endregion

            #region Send Email
            var reserveStudyEmail = await _reserveStudyService.MapToReserveStudyEmailAsync(reserveStudy!, 
                "A reserve study request has been shared with you. You can view the details online or contact the assigned specialist for more information.");
            
            // Build subject with tenant company name
            var companyName = reserveStudyEmail.TenantInfo?.CompanyName ?? "ALX Reserve Cloud";
            var subject = $"[{companyName}] Reserve Study Request for {reserveStudy?.Community?.Name}";
            
            var emailRecipients = new List<string>();
            
            // Add specialist if assigned
            if (!string.IsNullOrEmpty(reserveStudy?.Specialist?.Email)) {
                emailRecipients.Add(reserveStudy.Specialist.Email);
            }

            // Add point of contact
            if (!string.IsNullOrEmpty(reserveStudy?.PointOfContact?.Email)) {
                emailRecipients.Add(reserveStudy.PointOfContact.Email);
            }
            
            // Fallback to tenant's default notification email
            if (emailRecipients.Count == 0 && !string.IsNullOrEmpty(reserveStudyEmail.TenantInfo?.DefaultNotificationEmail)) {
                emailRecipients.Add(reserveStudyEmail.TenantInfo.DefaultNotificationEmail);
            }

            await _mailer.SendAsync(
                Mailable.AsInline<ReserveStudyEmail>()
                    .To(emailRecipients.ToArray())
                    .Subject(subject)
                    .ReplyToTenant(reserveStudyEmail.TenantInfo)
                    .View("~/Components/EmailTemplates/ReserveStudyCreate.cshtml", reserveStudyEmail)
            );
            #endregion
        }
    }
}
