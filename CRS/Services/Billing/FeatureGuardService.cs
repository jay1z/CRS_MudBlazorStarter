using CRS.Data;
using Microsoft.EntityFrameworkCore;
using CRS.Models;

namespace CRS.Services.Billing {
    public interface IFeatureGuardService {
        Task<bool> CanAddCommunityAsync(int tenantId, CancellationToken ct = default);
        Task<bool> CanAddSpecialistUserAsync(int tenantId, CancellationToken ct = default);
    }

    public class FeatureGuardService : IFeatureGuardService {
        private readonly ApplicationDbContext _db;
        public FeatureGuardService(ApplicationDbContext db) { _db = db; }

        public async Task<bool> CanAddCommunityAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);
            if (!tenant.IsActive) return false;
            if (tenant.MaxCommunities <= 0) return false;
            var count = await _db.Communities.CountAsync(c => c.TenantId == tenantId, ct);
            return count < tenant.MaxCommunities;
        }

        public async Task<bool> CanAddSpecialistUserAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, ct);
            if (!tenant.IsActive) return false;
            if (tenant.MaxSpecialistUsers <= 0) return false;
            // Count users via assignments table where role is TenantSpecialist or TenantOwner
            var specialistCount = await _db.UserRoleAssignments
                .Include(a => a.Role)
                .CountAsync(a => a.TenantId == tenantId && (a.Role.Name == "TenantSpecialist" || a.Role.Name == "TenantOwner"), ct);
            return specialistCount < tenant.MaxSpecialistUsers;
        }
    }
}
