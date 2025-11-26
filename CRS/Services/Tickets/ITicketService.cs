using CRS.Models;

namespace CRS.Services.Tickets {
    public interface ITicketService {
        Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken ct = default);
        Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, CancellationToken ct = default);
        Task<bool> CloseTicketAsync(Guid ticketId, string? closedBy = null, CancellationToken ct = default);
    }
}