using CRS.Components.AppHome.ViewModels;
using CRS.Data;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Tenant {
    public interface ITenantHomepageDataService {
        Task<TenantKpisVm?> GetKpisAsync(CancellationToken ct=default);
        Task<IReadOnlyList<PipelineStageVm>> GetPipelineAsync(bool specialistFiltered, Guid? userId, CancellationToken ct=default);
        Task<IReadOnlyList<WorkItemVm>> GetMyWorkAsync(Guid? userId, CancellationToken ct=default);
    }

    public class TenantHomepageDataService : ITenantHomepageDataService {
        private readonly ITenantContext _tenant;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public TenantHomepageDataService(ITenantContext tenant, IDbContextFactory<ApplicationDbContext> dbFactory) { _tenant = tenant; _dbFactory = dbFactory; }

        public async Task<TenantKpisVm?> GetKpisAsync(CancellationToken ct=default) {
            if (!_tenant.TenantId.HasValue) return null;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tid = _tenant.TenantId.Value;
            var properties = await db.Communities.CountAsync(c => c.TenantId == tid, ct);
            var studiesActive = await db.ReserveStudies.CountAsync(r => r.Community != null && r.Community.TenantId == tid && !r.IsComplete, ct);
            var reportsRecent = await db.ReserveStudies.CountAsync(r => r.Community != null && r.Community.TenantId == tid && r.IsComplete && r.DateCreated > DateTime.UtcNow.AddMonths(-6), ct);
            var accounts = 0; // TODO: implement CustomerAccounts count when model is available
            return new TenantKpisVm { Properties = properties, StudiesActive = studiesActive, ReportsRecent = reportsRecent, CustomerAccounts = accounts };
        }

        public async Task<IReadOnlyList<PipelineStageVm>> GetPipelineAsync(bool specialistFiltered, Guid? userId, CancellationToken ct=default) {
            var result = new List<PipelineStageVm>();
            if (!_tenant.TenantId.HasValue) return result;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tid = _tenant.TenantId.Value;
            // Example statuses - adapt to real model
            var query = db.ReserveStudies
                .Include(r => r.Community)
                .Where(r => r.Community != null && r.Community.TenantId == tid);
            if (specialistFiltered && userId.HasValue) {
                query = query.Where(r => r.SpecialistUserId == userId.Value);
            }
            var grouped = await query.GroupBy(r => r.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct);
            result.AddRange(grouped.Select(g => new PipelineStageVm { Stage = g.Status, Count = g.Count }));
            return result;
        }

        public async Task<IReadOnlyList<WorkItemVm>> GetMyWorkAsync(Guid? userId, CancellationToken ct=default) {
            var items = new List<WorkItemVm>();
            if (!_tenant.TenantId.HasValue || !userId.HasValue) return items;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tid = _tenant.TenantId.Value;
            var studies = await db.ReserveStudies
                .Include(r => r.Community)
                .Where(r => r.Community != null && r.Community.TenantId == tid && r.SpecialistUserId == userId.Value && !r.IsComplete)
                .OrderByDescending(r => r.DateCreated)
                .Take(25)
                .ToListAsync(ct);
            items.AddRange(studies.Select(s => new WorkItemVm {
                Id = s.Id,
                Title = s.Community?.Name ?? s.Id.ToString(),
                Property = s.Community?.Name ?? string.Empty,
                Status = s.Status.ToString(),
                DueDate = s.DateCreated?.AddDays(30)
            }));
            return items;
        }
    }
}
