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
[Route("api/checkin")]
[Authorize]
public class CheckinController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CheckinController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record ScanRequest(string QrCode);
    public record CreateStaffRequest(int EventId, string Name, string Email, string? Password);

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var issuedTicket = await _db.IssuedTickets
            .Include(it => it.Ticket).ThenInclude(t => t.Event)
            .Include(it => it.Booking)
            .FirstOrDefaultAsync(it => it.QrCode == request.QrCode);

        if (issuedTicket == null)
            return NotFound(ApiResponse.Fail("Invalid QR code"));

        if (issuedTicket.Booking.PaymentStatus != PaymentStatus.Completed)
            return BadRequest(ApiResponse.Fail("Payment not completed for this booking"));

        if (issuedTicket.IsCheckedIn)
            return BadRequest(ApiResponse<object>.Fail($"Already checked in at {issuedTicket.CheckedInAt:g}"));

        issuedTicket.IsCheckedIn = true;
        issuedTicket.CheckedInAt = DateTime.UtcNow;
        issuedTicket.CheckedInBy = userId.Value;

        // Check if all tickets in booking are checked in
        var allCheckedIn = await _db.IssuedTickets
            .Where(it => it.BookingId == issuedTicket.BookingId && it.Id != issuedTicket.Id)
            .AllAsync(it => it.IsCheckedIn);

        if (allCheckedIn)
        {
            issuedTicket.Booking.IsCheckedIn = true;
            issuedTicket.Booking.CheckedInAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            issuedTicket.AttendeeName,
            issuedTicket.AttendeeEmail,
            ticketName = issuedTicket.Ticket.Name,
            ticketType = issuedTicket.Ticket.Type.ToString().ToLowerInvariant(),
            eventName = issuedTicket.Ticket.Event.Name,
            checkedInAt = issuedTicket.CheckedInAt
        }, "Check-in successful"));
    }

    [HttpGet("event/{eventId:int}/stats")]
    public async Task<IActionResult> GetStats(int eventId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var issuedTickets = await _db.IssuedTickets
            .Include(it => it.Ticket)
            .Include(it => it.Booking)
            .Where(it => it.Ticket.EventId == eventId && it.Booking.PaymentStatus == PaymentStatus.Completed)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            eventId,
            eventName = ev.Name,
            totalTickets = issuedTickets.Count,
            checkedIn = issuedTickets.Count(it => it.IsCheckedIn),
            notCheckedIn = issuedTickets.Count(it => !it.IsCheckedIn),
            percentageCheckedIn = issuedTickets.Count > 0
                ? Math.Round((double)issuedTickets.Count(it => it.IsCheckedIn) / issuedTickets.Count * 100, 1)
                : 0
        }));
    }

    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found or not owned by you"));

        var staff = new StaffMember
        {
            OrganizerId = userId.Value,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = !string.IsNullOrEmpty(request.Password)
                ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                : null
        };

        _db.StaffMembers.Add(staff);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStaff), null,
            ApiResponse<object>.Ok(new { staff.Id, staff.Name, staff.Email }, "Staff created"));
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var staff = await _db.StaffMembers
            .Where(s => s.OrganizerId == userId.Value && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(staff.Select(s => new
        {
            s.Id,
            s.Name,
            s.Email,
            s.IsActive,
            s.CreatedAt
        })));
    }
}
