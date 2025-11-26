using CRS.Data;
using CRS.Models;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Tickets {
    public class TicketService : ITicketService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenant;
        public TicketService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenant) { _dbFactory = dbFactory; _tenant = tenant; }

        public async Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) return Array.Empty<SupportTicket>();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.SupportTickets.AsNoTracking().Where(t => t.TenantId == _tenant.TenantId && t.Status != "Closed").OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        }

        public async Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) throw new InvalidOperationException("Tenant context required");
            ticket.TenantId = _tenant.TenantId.Value;
            ticket.CreatedAt = DateTime.UtcNow;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync(ct);
            return ticket;
        }

        public async Task<bool> CloseTicketAsync(Guid ticketId, string? closedBy = null, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var t = await db.SupportTickets.FirstOrDefaultAsync(s => s.Id == ticketId, ct);
            if (t == null) return false;
            t.Status = "Closed";
            t.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}