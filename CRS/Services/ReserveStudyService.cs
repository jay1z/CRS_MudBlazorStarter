using System.Globalization;

using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

using MudBlazor;

using static CRS.Components.Pages.Dashboard.Index;

namespace CRS.Services {
    public class ReserveStudyService : IReserveStudyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private IDispatcher _dispatcher;

        public ReserveStudyService(IDbContextFactory<ApplicationDbContext> dbFactory, IMailer mailer, IDispatcher dispatcher) {
            _dbFactory = dbFactory;
            _dispatcher = dispatcher;
        }

        public async Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy) {
            using var context = await _dbFactory.CreateDbContextAsync();

            // Set default values for a new study
            reserveStudy.IsActive = true;
            reserveStudy.IsApproved = false;
            reserveStudy.IsComplete = false;

            // Add to database
            context.ReserveStudies.Add(reserveStudy);
            await context.SaveChangesAsync();

            // Send notification email
            // await SendReserveStudyCreatedEmailAsync(reserveStudy);

            var createdEvent = new ReserveStudyCreatedEvent(reserveStudy);
            await _dispatcher.Broadcast<ReserveStudyCreatedEvent>(createdEvent);

            return reserveStudy;
        }

        public async Task<bool> DeleteReserveStudyAsync(Guid id) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var reserveStudy = await context.ReserveStudies
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (reserveStudy == null) {
                return false;
            }

            // Option 1: Hard delete (completely remove from database)
            // context.ReserveStudies.Remove(reserveStudy);

            // Option 2: Soft delete (mark as inactive)
            reserveStudy.IsActive = false;
            reserveStudy.DateDeleted = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        public ReserveStudyEmail MapToReserveStudyEmail(ReserveStudy reserveStudy, string message) {
            return new ReserveStudyEmail {
                ReserveStudy = reserveStudy,
                BaseUrl = "https://yourdomain.com", // Replace with your actual base URL
                AdditionalMessage = message
            };
        }

        public async Task<List<ReserveStudy>> GetAllReserveStudiesAsync() {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId) {
            //if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var specialistId)) {
            //    return new List<ReserveStudy>();
            //}
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId) //specialistId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId) {
            //if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerId)) {
            //    return new List<ReserveStudy>();
            //}
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive && rs.ApplicationUserId == userId) //ownerId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        #region Token Access
        public async Task<ReserveStudy?> GetStudyByTokenAsync(string tokenStr) {
            if (!Guid.TryParse(tokenStr, out var token)) {
                return null;
            }

            // First validate the token and get the associated request ID
            var requestId = await ValidateAccessTokenAsync(token);
            if (requestId == null) {
                return null;
            }

            using var context = await _dbFactory.CreateDbContextAsync();

            // Fetch the reserve study with the request ID
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.BuildingElement)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.CommonElement)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyAdditionalElements!).ThenInclude(be => be.ServiceContact)
                .AsSplitQuery()
                .FirstOrDefaultAsync(rs => rs.Id == requestId && rs.IsActive);
        }

        public async Task<Guid> GenerateAccessTokenAsync(Guid requestId) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var token = Guid.CreateVersion7();

            var accessToken = new AccessToken {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                RequestId = requestId
            };

            context.AccessTokens.Add(accessToken);
            await context.SaveChangesAsync();

            return token;
        }

        public async Task<Guid?> ValidateAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.Expiration > DateTime.UtcNow);

            return accessToken?.RequestId;
        }

        public async Task RevokeAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (accessToken != null) {
                context.AccessTokens.Remove(accessToken);
                await context.SaveChangesAsync();
            }
        }
        #endregion
    }
}
