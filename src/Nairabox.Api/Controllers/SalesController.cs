using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SalesController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("event/{eventId:int}/summary")]
    public async Task<IActionResult> GetEventSummary(int eventId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var bookings = await _db.Bookings
            .Where(b => b.EventId == eventId && b.PaymentStatus == PaymentStatus.Completed)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            eventId,
            eventName = ev.Name,
            totalRevenue = bookings.Sum(b => b.TotalAmount),
            totalServiceFees = bookings.Sum(b => b.ServiceFee),
            totalDiscounts = bookings.Sum(b => b.DiscountAmount),
            netRevenue = bookings.Sum(b => b.TotalAmount - b.ServiceFee),
            totalBookings = bookings.Count,
            totalTicketsSold = bookings.Sum(b => b.TicketQuantity)
        }));
    }

    [HttpGet("event/{eventId:int}/breakdown")]
    public async Task<IActionResult> GetTicketBreakdown(int eventId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var tickets = await _db.Tickets
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        var breakdown = tickets.Select(t => new
        {
            t.Id,
            t.Name,
            type = t.Type.ToString().ToLowerInvariant(),
            t.Quantity,
            t.QuantitySold,
            available = t.Quantity - t.QuantitySold,
            t.Price,
            revenue = t.QuantitySold * t.Price,
            percentageSold = t.Quantity > 0 ? Math.Round((double)t.QuantitySold / t.Quantity * 100, 1) : 0
        });

        return Ok(ApiResponse<object>.Ok(breakdown));
    }

    [HttpGet("event/{eventId:int}/records")]
    public async Task<IActionResult> GetBookingRecords(int eventId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var query = _db.Bookings
            .Where(b => b.EventId == eventId)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var bookings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<object>
        {
            Items = bookings.Select(b => new
            {
                b.Id,
                b.BookingId,
                b.CustomerName,
                b.CustomerEmail,
                b.TotalAmount,
                paymentStatus = b.PaymentStatus.ToString().ToLowerInvariant(),
                b.TicketQuantity,
                b.IsCheckedIn,
                b.CreatedAt
            } as object).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<object>>.Ok(result));
    }

    [HttpGet("organizer/summary")]
    public async Task<IActionResult> GetOrganizerSummary()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var events = await _db.Events
            .Where(e => e.OrganizerId == userId.Value)
            .ToListAsync();

        var eventIds = events.Select(e => e.Id).ToList();

        var bookings = await _db.Bookings
            .Where(b => eventIds.Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            totalEvents = events.Count,
            publishedEvents = events.Count(e => e.Status == EventStatus.Published),
            draftEvents = events.Count(e => e.Status == EventStatus.Draft),
            totalRevenue = bookings.Sum(b => b.TotalAmount),
            totalBookings = bookings.Count,
            totalTicketsSold = bookings.Sum(b => b.TicketQuantity)
        }));
    }
}
