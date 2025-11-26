using CRS.Models;
using CRS.Services.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers {
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase {
        private readonly ITicketService _tickets;
        public TicketsController(ITicketService tickets) { _tickets = tickets; }

        [HttpGet("open")]
        public async Task<IActionResult> GetOpen(CancellationToken ct) {
            var list = await _tickets.GetOpenTicketsAsync(ct);
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupportTicket ticket, CancellationToken ct) {
            if (ticket == null) return BadRequest("ticket required");
            var created = await _tickets.CreateTicketAsync(ticket, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct) {
            var list = await _tickets.GetOpenTicketsAsync(ct);
            var t = list.FirstOrDefault(x => x.Id == id);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpPost("{id:guid}/close")]
        public async Task<IActionResult> Close(Guid id, CancellationToken ct) {
            var ok = await _tickets.CloseTicketAsync(id, null, ct);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPost("{id:guid}/assign")]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketRequest req, CancellationToken ct) {
            // Placeholder: update ticket assigned to user
            // In real app, update service to set AssignedTo
            return Ok(new { message = "Assigned" });
        }

        public record AssignTicketRequest(string AssignedTo);
    }
}