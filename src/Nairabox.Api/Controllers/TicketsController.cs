using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TicketsController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record CreateTicketRequest(
        int EventId,
        string Name,
        string? Description,
        string? Type,
        int Quantity,
        decimal Price,
        int? PurchaseLimit,
        DateTime? SalesStartDate,
        DateTime? SalesEndDate,
        bool IsRefundable,
        bool TransferFeesToGuest,
        bool IsInviteOnly,
        string? InviteOnlyPassword);

    public record UpdateTicketRequest(
        string? Name,
        string? Description,
        string? Type,
        int? Quantity,
        decimal? Price,
        int? PurchaseLimit,
        DateTime? SalesStartDate,
        DateTime? SalesEndDate,
        bool? IsRefundable,
        bool? TransferFeesToGuest,
        bool? IsInviteOnly,
        string? InviteOnlyPassword);

    [HttpGet("by-event/{eventId:int}")]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var tickets = await _db.Tickets
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(tickets.Select(t => new
        {
            t.Id,
            t.EventId,
            t.Name,
            t.Description,
            type = t.Type.ToString().ToLowerInvariant(),
            t.Quantity,
            t.QuantitySold,
            t.Price,
            t.PurchaseLimit,
            t.SalesStartDate,
            t.SalesEndDate,
            t.IsRefundable,
            t.TransferFeesToGuest,
            t.IsInviteOnly
        })));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found or not owned by you"));

        var ticket = new Ticket
        {
            EventId = request.EventId,
            Name = request.Name,
            Description = request.Description,
            Type = ParseEnum<TicketType>(request.Type, TicketType.Free),
            Quantity = request.Quantity,
            Price = request.Price,
            PurchaseLimit = request.PurchaseLimit,
            SalesStartDate = request.SalesStartDate,
            SalesEndDate = request.SalesEndDate,
            IsRefundable = request.IsRefundable,
            TransferFeesToGuest = request.TransferFeesToGuest,
            IsInviteOnly = request.IsInviteOnly,
            InviteOnlyPassword = request.InviteOnlyPassword
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByEvent), new { eventId = request.EventId },
            ApiResponse<object>.Ok(new { ticket.Id }, "Ticket created"));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ticket = await _db.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound(ApiResponse.Fail("Ticket not found"));
        if (ticket.Event.OrganizerId != userId.Value) return Forbid();

        if (request.Name != null) ticket.Name = request.Name;
        if (request.Description != null) ticket.Description = request.Description;
        if (request.Type != null) ticket.Type = ParseEnum<TicketType>(request.Type, ticket.Type);
        if (request.Quantity.HasValue) ticket.Quantity = request.Quantity.Value;
        if (request.Price.HasValue) ticket.Price = request.Price.Value;
        if (request.PurchaseLimit.HasValue) ticket.PurchaseLimit = request.PurchaseLimit.Value;
        if (request.SalesStartDate.HasValue) ticket.SalesStartDate = request.SalesStartDate.Value;
        if (request.SalesEndDate.HasValue) ticket.SalesEndDate = request.SalesEndDate.Value;
        if (request.IsRefundable.HasValue) ticket.IsRefundable = request.IsRefundable.Value;
        if (request.TransferFeesToGuest.HasValue) ticket.TransferFeesToGuest = request.TransferFeesToGuest.Value;
        if (request.IsInviteOnly.HasValue) ticket.IsInviteOnly = request.IsInviteOnly.Value;
        if (request.InviteOnlyPassword != null) ticket.InviteOnlyPassword = request.InviteOnlyPassword;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Ticket updated"));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ticket = await _db.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound(ApiResponse.Fail("Ticket not found"));
        if (ticket.Event.OrganizerId != userId.Value) return Forbid();

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Ticket deleted"));
    }

    private static T ParseEnum<T>(string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }
}
