using Horizon.Data;
using Microsoft.EntityFrameworkCore;
using Horizon.Models;

namespace Horizon.Services.Billing {
    /// <summary>
    /// DTO for returning current usage and limits
    /// </summary>
    public record UsageInfo(int CurrentCount, int MaxAllowed, bool CanAdd, string? LimitReachedMessage = null);

    public interface IFeatureGuardService {
        Task<bool> CanAddCommunityAsync(int tenantId, CancellationToken ct = default);
        Task<bool> CanAddSpecialistUserAsync(int tenantId, CancellationToken ct = default);
        Task<UsageInfo> GetCommunityUsageAsync(int tenantId, CancellationToken ct = default);
        Task<UsageInfo> GetSpecialistUserUsageAsync(int tenantId, CancellationToken ct = default);
    }

    public class FeatureGuardService : IFeatureGuardService {
        private readonly ApplicationDbContext _db;
        public FeatureGuardService(ApplicationDbContext db) { _db = db; }

        public async Task<bool> CanAddCommunityAsync(int tenantId, CancellationToken ct = default) {
            var usage = await GetCommunityUsageAsync(tenantId, ct);
            return usage.CanAdd;
        }

        public async Task<bool> CanAddSpecialistUserAsync(int tenantId, CancellationToken ct = default) {
            var usage = await GetSpecialistUserUsageAsync(tenantId, ct);
            return usage.CanAdd;
        }

        public async Task<UsageInfo> GetCommunityUsageAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) {
                return new UsageInfo(0, 0, false, "Tenant not found.");
            }
            if (!tenant.IsActive) {
                return new UsageInfo(0, tenant.MaxCommunities, false, "Tenant account is not active.");
            }

            var count = await _db.Communities
                .IgnoreQueryFilters()
                .CountAsync(c => c.TenantId == tenantId && c.IsActive, ct);

            var canAdd = tenant.MaxCommunities > 0 && count < tenant.MaxCommunities;
            var message = canAdd ? null : $"Community limit reached ({count}/{tenant.MaxCommunities}). Please upgrade your plan to add more communities.";

            return new UsageInfo(count, tenant.MaxCommunities, canAdd, message);
        }

        public async Task<UsageInfo> GetSpecialistUserUsageAsync(int tenantId, CancellationToken ct = default) {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) {
                return new UsageInfo(0, 0, false, "Tenant not found.");
            }
            if (!tenant.IsActive) {
                return new UsageInfo(0, tenant.MaxSpecialistUsers, false, "Tenant account is not active.");
            }

            // Count users via assignments table where role is TenantSpecialist or TenantOwner
            var count = await _db.UserRoleAssignments
                .Include(a => a.Role)
                .Where(a => a.TenantId == tenantId && (a.Role!.Name == "TenantSpecialist" || a.Role!.Name == "TenantOwner"))
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(ct);

            var canAdd = tenant.MaxSpecialistUsers > 0 && count < tenant.MaxSpecialistUsers;
            var message = canAdd ? null : $"Team member limit reached ({count}/{tenant.MaxSpecialistUsers}). Please upgrade your plan to add more team members.";

            return new UsageInfo(count, tenant.MaxSpecialistUsers, canAdd, message);
        }
    }
}
