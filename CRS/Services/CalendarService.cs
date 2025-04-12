using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class CalendarService : ICalendarService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public CalendarService(IDbContextFactory<ApplicationDbContext> dbContextFactory) {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<CalendarEvent?> GetEventAsync(Guid id) {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.CalendarEvents
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CalendarEvent>> GetEventsAsync(DateTime? startDate = null, DateTime? endDate = null) {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.CalendarEvents.Where(e => !e.IsDeleted);

            if (startDate.HasValue) {
                query = query.Where(e => e.Start >= startDate);
            }

            if (endDate.HasValue) {
                query = query.Where(e => e.Start <= endDate);
            }

            return await query.ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetUserEventsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null) {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.CalendarEvents
                .Where(e => !e.IsDeleted &&
                           (e.ApplicationUserId == userId || e.SpecialistUserId == userId || e.IsPublic));

            if (startDate.HasValue) {
                query = query.Where(e => e.Start >= startDate);
            }

            if (endDate.HasValue) {
                query = query.Where(e => e.Start <= endDate);
            }

            return await query.ToListAsync();
        }

        public async Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent) {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            // Set default values for new events if needed
            calendarEvent.IsDeleted = false;
            calendarEvent.IsVisible = true;
            calendarEvent.IsEditable = true;

            if (calendarEvent.End == null && calendarEvent.Start != null) {
                calendarEvent.End = calendarEvent.Start.Value.AddHours(1);
            }

            await context.CalendarEvents.AddAsync(calendarEvent);
            await context.SaveChangesAsync();

            return calendarEvent;
        }

        public async Task<CalendarEvent?> UpdateEventAsync(CalendarEvent calendarEvent) {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var existingEvent = await context.CalendarEvents
                .Where(e => e.Id == calendarEvent.Id && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (existingEvent == null) {
                return null;
            }

            // Update properties
            existingEvent.Title = calendarEvent.Title;
            existingEvent.Description = calendarEvent.Description;
            existingEvent.Location = calendarEvent.Location;
            existingEvent.Start = calendarEvent.Start;
            existingEvent.End = calendarEvent.End;
            existingEvent.IsAllDay = calendarEvent.IsAllDay;
            existingEvent.Color = calendarEvent.Color;
            existingEvent.IsPublic = calendarEvent.IsPublic;
            existingEvent.IsPrivate = calendarEvent.IsPrivate;
            existingEvent.IsRecurring = calendarEvent.IsRecurring;

            // Update any other properties as needed

            context.CalendarEvents.Update(existingEvent);
            await context.SaveChangesAsync();

            return existingEvent;
        }

        public async Task<bool> DeleteEventAsync(Guid id) {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var calendarEvent = await context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null) {
                return false;
            }

            context.CalendarEvents.Remove(calendarEvent);
            var result = await context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> SoftDeleteEventAsync(Guid id) {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var calendarEvent = await context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null) {
                return false;
            }

            calendarEvent.IsDeleted = true;
            context.CalendarEvents.Update(calendarEvent);
            var result = await context.SaveChangesAsync();

            return result > 0;
        }
    }
}
