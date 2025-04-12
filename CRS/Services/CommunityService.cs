using CRS.Data;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
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

    }
}
