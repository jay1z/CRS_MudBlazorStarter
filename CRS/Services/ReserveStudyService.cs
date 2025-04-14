using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class ReserveStudyService : IReserveStudyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IDispatcher _dispatcher;
        private readonly string _baseUrl;

        public ReserveStudyService(IDbContextFactory<ApplicationDbContext> dbFactory, IMailer mailer, IDispatcher dispatcher, IConfiguration configuration) {
            _dbFactory = dbFactory;
            _dispatcher = dispatcher;
            _baseUrl = configuration["Application:BaseUrl"] ?? "https://yourdomain.com";
        }

        public async Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy) {
            using var context = await _dbFactory.CreateDbContextAsync();

            // Set default values for a new study
            reserveStudy.IsActive = true;
            reserveStudy.IsApproved = false;
            reserveStudy.IsComplete = false;

            // Handle Contact
            if (reserveStudy.Contact != null && reserveStudy.ContactId == null) {
                // This is a new contact
                reserveStudy.Contact.Id = Guid.CreateVersion7(); // Generate new ID
            }
            else if (reserveStudy.ContactId != null) {
                // This is a reference to existing contact - detach entity
                reserveStudy.Contact = null;
            }

            // Handle PropertyManager
            if (reserveStudy.PropertyManager != null && reserveStudy.PropertyManagerId == null) {
                // This is a new property manager
                reserveStudy.PropertyManager.Id = Guid.CreateVersion7(); // Generate new ID
            }
            else if (reserveStudy.PropertyManagerId != null) {
                // This is a reference to existing property manager - detach entity
                reserveStudy.PropertyManager = null;
            }

            // Add to database
            context.ReserveStudies.Add(reserveStudy);
            await context.SaveChangesAsync();

            // Get community data for the email
            var community = await context.Communities
                .AsNoTracking()
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == reserveStudy.CommunityId);

            if (community != null) {
                reserveStudy.Community = community;
            }

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

            reserveStudy.IsActive = false;
            reserveStudy.DateDeleted = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets all active reserve studies
        /// </summary>
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

        /// <summary>
        /// Gets reserve studies assigned to a specialist
        /// </summary>
        public async Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId) {
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
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies owned by a user
        /// </summary>
        public async Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId) {
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
                .Where(rs => rs.IsActive && rs.ApplicationUserId == userId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies for the current user based on their role and permissions
        /// </summary>
        public async Task<List<ReserveStudy>> GetUserReserveStudiesAsync(ApplicationUser currentUser, IList<string> userRoles) {
            if (currentUser == null || userRoles == null) {
                return new List<ReserveStudy>();
            }

            try {
                // Based on role, call the appropriate method
                if (userRoles.Contains("Admin")) {
                    return await GetAllReserveStudiesAsync();
                }
                else if (userRoles.Contains("Specialist")) {
                    return await GetAssignedReserveStudiesAsync(currentUser.Id);
                }
                else if (userRoles.Contains("User")) {
                    return await GetOwnedReserveStudiesAsync(currentUser.Id);
                }
                else {
                    return new List<ReserveStudy>();
                }
            }
            catch (Exception) {
                // Log exception if you have a logging mechanism
                return new List<ReserveStudy>();
            }
        }

        /// <summary>
        /// Maps reserve study data to an email model
        /// </summary>
        public ReserveStudyEmail MapToReserveStudyEmail(ReserveStudy reserveStudy, string message) {
            return new ReserveStudyEmail {
                ReserveStudy = reserveStudy,
                BaseUrl = _baseUrl,
                AdditionalMessage = message
            };
        }

        /// <summary>
        /// Filter reserve studies based on a search string
        /// </summary>
        public IEnumerable<ReserveStudy> FilterReserveStudies(IEnumerable<ReserveStudy> reserveStudies, string searchString) {
            if (string.IsNullOrWhiteSpace(searchString)) {
                return reserveStudies;
            }

            searchString = searchString.Trim().ToLower();
            return reserveStudies.Where(study =>
                study.Community?.Name?.ToLower().Contains(searchString) == true ||
                study.Contact?.FullName?.ToLower().Contains(searchString) == true ||
                study.PropertyManager?.FullName?.ToLower().Contains(searchString) == true ||
                study.Community?.Addresses?.Any(address =>
                    address.Street?.ToLower().Contains(searchString) == true ||
                    address.City?.ToLower().Contains(searchString) == true ||
                    address.State?.ToLower().Contains(searchString) == true ||
                    address.Zip?.ToLower().Contains(searchString) == true
                ) == true
            );
        }

        #region Token Access
        /// <summary>
        /// Gets a reserve study using a token for external access
        /// </summary>
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

        /// <summary>
        /// Generates a temporary access token for a reserve study
        /// </summary>
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

        /// <summary>
        /// Validates an access token and returns the associated request ID if valid
        /// </summary>
        public async Task<Guid?> ValidateAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            return await context.AccessTokens
                .Where(t => t.Token == token && t.Expiration > DateTime.UtcNow)
                .Select(t => t.RequestId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Revokes an access token
        /// </summary>
        public async Task RevokeAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (accessToken != null) {
                context.AccessTokens.Remove(accessToken);
                await context.SaveChangesAsync();
            }
        }
        /// <summary>
        /// Gets a reserve study by its ID
        /// </summary>
        public async Task<ReserveStudy?> GetReserveStudyByIdAsync(Guid studyId) {
            using var context = await _dbFactory.CreateDbContextAsync();

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
                .FirstOrDefaultAsync(rs => rs.Id == studyId && rs.IsActive);
        }
        #endregion
    }
}
