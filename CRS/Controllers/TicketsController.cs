using CRS.Models;
using CRS.Services.Tickets;
using CRS.Services.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRS.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly ITenantUserRoleService _roleService;

    public TicketsController(ITicketService tickets, ITenantUserRoleService roleService)
    {
        _tickets = tickets;
        _roleService = roleService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private async Task<bool> IsStaffAsync()
    {
        await _roleService.InitializeAsync();
        return _roleService.IsTenantOwner || _roleService.IsTenantSpecialist ||
               _roleService.IsPlatformAdmin || _roleService.IsPlatformSupport;
    }

    #region Query Endpoints

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(CancellationToken ct)
    {
        var list = await _tickets.GetOpenTicketsAsync(ct);
        return Ok(list);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TicketFilter? filter, CancellationToken ct)
    {
        var list = await _tickets.GetAllTicketsAsync(filter, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var ticket = await _tickets.GetTicketByIdAsync(id, ct);
        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    [HttpGet("platform")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<IActionResult> GetAllForPlatform([FromQuery] TicketFilter? filter, CancellationToken ct)
    {
        var list = await _tickets.GetAllTicketsForPlatformAsync(filter, ct);
        return Ok(list);
    }

    #endregion

    #region Lifecycle Endpoints

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        var ticket = new SupportTicket
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Priority = request.Priority ?? TicketPriority.Medium,
            Category = request.Category ?? TicketCategory.General,
            ReserveStudyId = request.ReserveStudyId
        };

        var created = await _tickets.CreateTicketAsync(ticket, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TicketUpdateRequest update, CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        var ok = await _tickets.UpdateTicketAsync(id, update, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketRequest req, CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        Guid? assignedToId = null;
        if (!string.IsNullOrEmpty(req.AssignedToUserId))
        {
            if (!Guid.TryParse(req.AssignedToUserId, out var parsed))
                return BadRequest("Invalid user ID");
            assignedToId = parsed;
        }

        var ok = await _tickets.AssignTicketAsync(id, assignedToId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveTicketRequest? req, CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        var ok = await _tickets.ResolveTicketAsync(id, req?.Resolution, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] ResolveTicketRequest? req, CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        var ok = await _tickets.CloseTicketAsync(id, req?.Resolution, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        var ok = await _tickets.ReopenTicketAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    #endregion

    #region Comment Endpoints

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct)
    {
        var isStaff = await IsStaffAsync();
        var comments = await _tickets.GetCommentsAsync(id, includeInternal: isStaff, ct);
        return Ok(comments);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Content))
            return BadRequest("Content is required");

        var isStaff = await IsStaffAsync();
        
        // Non-staff users can only create public comments
        var visibility = isStaff && req.IsInternal ? CommentVisibility.Internal : CommentVisibility.Public;

        var comment = await _tickets.AddCommentAsync(id, req.Content, GetCurrentUserId(), isStaff, visibility, ct);
        return CreatedAtAction(nameof(GetComments), new { id }, comment);
    }

    #endregion

    #region Staff Endpoints

    [HttpGet("assignable-users")]
    public async Task<IActionResult> GetAssignableUsers(CancellationToken ct)
    {
        if (!await IsStaffAsync())
            return Forbid();

        var users = await _tickets.GetAssignableUsersAsync(ct);
        return Ok(users.Select(u => new { u.Id, u.Email, u.FirstName, u.LastName }));
    }

    #endregion
}

#region Request DTOs

public record CreateTicketRequest(
    string Title,
    string? Description,
    TicketPriority? Priority,
    TicketCategory? Category,
    Guid? ReserveStudyId
);

public record AssignTicketRequest(string AssignedToUserId);

public record ResolveTicketRequest(string? Resolution);

public record AddCommentRequest(string Content, bool IsInternal = false);

#endregion