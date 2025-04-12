using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface ICalendarService {
        Task<CalendarEvent?> GetEventAsync(Guid id);
        Task<List<CalendarEvent>> GetEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<CalendarEvent>> GetUserEventsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent);
        Task<CalendarEvent?> UpdateEventAsync(CalendarEvent calendarEvent);
        Task<bool> DeleteEventAsync(Guid id);
        Task<bool> SoftDeleteEventAsync(Guid id);
    }
}
