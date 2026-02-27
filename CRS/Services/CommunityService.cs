using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services {
    public class CommunityService : ICommunityService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public CommunityService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }

        public async Task<bool> DeleteCommunityAsync(Guid id) {
            using var context = await _dbFactory.CreateDbContextAsync();
            var community = await context.Communities.FirstOrDefaultAsync(c => c.Id == id);

            if (community == null) {
                return false;
            }

            var reserveStudies = await context.ReserveStudies.Where(rs => rs.CommunityId == id).ToListAsync();

            community.IsActive = false;
            community.DateDeleted = DateTime.UtcNow;

            foreach (var reserveStudy in reserveStudies) {
                reserveStudy.IsActive = false;
                reserveStudy.DateDeleted = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets all active communities
        /// </summary>
        public async Task<List<Community>> GetAllCommunitiesAsync() {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Communities
                .AsNoTracking()
                .Include(c => c.PhysicalAddress)
                .Include(c => c.MailingAddress)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets communities assigned to a specialist
        /// </summary>
        public async Task<List<Community>> GetAssignedCommunitiesAsync(Guid userId) {
            if (userId == Guid.Empty) {
                return new List<Community>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();

            // Get communities through reserve studies assigned to this specialist
            var communityIds = await context.ReserveStudies
                .AsNoTracking()
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId)
                .Select(rs => rs.CommunityId)
                .Distinct()
                .ToListAsync();

            return await context.Communities
                    .AsNoTracking()
                    .Include(c => c.PhysicalAddress)
                    .Include(c => c.MailingAddress)
                    .Where(c => c.IsActive && communityIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .AsSplitQuery()
                    .ToListAsync();
            }

            /// <summary>
            /// Gets communities owned by a user
            /// </summary>
        public async Task<List<Community>> GetOwnedCommunitiesAsync(Guid userId) {
            if (userId == Guid.Empty) {
                return new List<Community>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();

            // Get communities through reserve studies owned by this user
            var communityIds = await context.ReserveStudies
                .AsNoTracking()
                .Where(rs => rs.IsActive && rs.ApplicationUserId == userId)
                .Select(rs => rs.CommunityId)
                .Distinct()
                .ToListAsync();

            return await context.Communities
                    .AsNoTracking()
                    .Include(c => c.PhysicalAddress)
                    .Include(c => c.MailingAddress)
                    .Where(c => c.IsActive && communityIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .AsSplitQuery()
                    .ToListAsync();
            }

            /// <summary>
            /// Gets communities for the current user based on their role and permissions
            /// </summary>
        public async Task<List<Community>> GetUserCommunitiesAsync(ApplicationUser currentUser, IList<string> userRoles) {
            if (currentUser == null || userRoles == null) {
                return new List<Community>();
            }

            try {
                // Based on role, call the appropriate method
                // PlatformAdmin or TenantOwner can see all communities
                if (userRoles.Contains("PlatformAdmin") || userRoles.Contains("TenantOwner")) {
                    return await GetAllCommunitiesAsync();
                }
                // TenantSpecialist sees assigned communities
                else if (userRoles.Contains("TenantSpecialist")) {
                    return await GetAssignedCommunitiesAsync(currentUser.Id);
                }
                // TenantViewer, HOAUser, or other authenticated users see their owned communities
                else if (userRoles.Contains("TenantViewer") || userRoles.Contains("HOAUser") || userRoles.Contains("HOAAuditor")) {
                    return await GetOwnedCommunitiesAsync(currentUser.Id);
                }
                else {
                    // Fallback: try to load owned communities for any authenticated user
                    return await GetOwnedCommunitiesAsync(currentUser.Id);
                }
            }
            catch (Exception) {
                // Log exception if you have a logging mechanism
                return new List<Community>();
            }
        }

        /// <summary>
        /// Filter communities based on a search string
        /// </summary>
        public IEnumerable<Community> FilterCommunities(IEnumerable<Community> communities, string searchString) {
            if (string.IsNullOrWhiteSpace(searchString)) {
                return communities;
            }

            searchString = searchString.Trim().ToLower();
                return communities.Where(community =>
                    community.Name?.ToLower().Contains(searchString) == true ||
                    community.PhysicalAddress?.Street?.ToLower().Contains(searchString) == true ||
                    community.PhysicalAddress?.City?.ToLower().Contains(searchString) == true ||
                    community.PhysicalAddress?.State?.ToLower().Contains(searchString) == true ||
                    community.PhysicalAddress?.Zip?.ToLower().Contains(searchString) == true
                );
            }
    }
}
