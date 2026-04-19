using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

/// <summary>
/// Public-facing event discovery endpoints (featured, popular, search, and detail).
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly IConfiguration _config;

    public EventsController(NairaboxDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Returns a list of featured published events.
    /// </summary>
    /// <returns>List of featured events with organizer, tickets, and categories</returns>
    /// <response code="200">Featured events retrieved</response>
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured()
    {
        var maxFeatured = _config.GetValue("AppSettings:MaxFeaturedEvents", 10);

        var events = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .Where(e => e.Status == EventStatus.Published && e.IsFeatured)
            .OrderByDescending(e => e.StartDate)
            .Take(maxFeatured)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events.Select(MapEvent)));
    }

    /// <summary>
    /// Returns a list of popular published events ordered by start date.
    /// </summary>
    /// <returns>List of popular events with organizer, tickets, and categories</returns>
    /// <response code="200">Popular events retrieved</response>
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
    {
        var maxPopular = _config.GetValue("AppSettings:MaxPopularEvents", 20);

        var events = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .Where(e => e.Status == EventStatus.Published)
            .OrderByDescending(e => e.StartDate)
            .Take(maxPopular)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events.Select(MapEvent)));
    }

    /// <summary>
    /// Returns published events filtered by category.
    /// </summary>
    /// <param name="categoryId">Category ID to filter by</param>
    /// <returns>List of events in the specified category</returns>
    /// <response code="200">Events retrieved</response>
    [HttpGet("by-category/{categoryId:int}")]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        var events = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .Where(e => e.Status == EventStatus.Published && e.CategoryMaps.Any(cm => cm.CategoryId == categoryId))
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events.Select(MapEvent)));
    }

    /// <summary>
    /// Searches published events by name or description.
    /// </summary>
    /// <param name="q">Search query string</param>
    /// <returns>List of matching events</returns>
    /// <response code="200">Search results returned</response>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(ApiResponse<object>.Ok(Array.Empty<object>()));

        var events = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .Where(e => e.Status == EventStatus.Published &&
                (EF.Functions.Like(e.Name, $"%{q}%") || EF.Functions.Like(e.Description!, $"%{q}%")))
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events.Select(MapEvent)));
    }

    /// <summary>
    /// Retrieves a single event by its URL slug.
    /// </summary>
    /// <param name="slug">Event URL slug</param>
    /// <returns>Full event details</returns>
    /// <response code="200">Event found</response>
    /// <response code="404">Event not found</response>
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var ev = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .FirstOrDefaultAsync(e => e.Slug == slug);

        if (ev == null)
            return NotFound(ApiResponse.Fail("Event not found"));

        return Ok(ApiResponse<object>.Ok(MapEvent(ev)));
    }

    /// <summary>
    /// Retrieves a single event by its numeric ID.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Full event details</returns>
    /// <response code="200">Event found</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound(ApiResponse.Fail("Event not found"));

        return Ok(ApiResponse<object>.Ok(MapEvent(ev)));
    }

    private static object MapEvent(Event e) => new
    {
        e.Id,
        e.OrganizerId,
        e.Name,
        e.Description,
        e.Slug,
        format = e.Format.ToString().ToLowerInvariant(),
        type = e.Type.ToString().ToLowerInvariant(),
        status = e.Status.ToString().ToLowerInvariant(),
        e.IsFeatured,
        e.StartDate,
        e.EndDate,
        e.Timezone,
        e.Location,
        e.VirtualLink,
        e.BannerImageUrl,
        e.ThumbnailImageUrl,
        e.TicketsSold,
        e.TotalRevenue,
        e.RecurringConfig,
        e.CreatedAt,
        e.PublishedAt,
        organizer = new { e.Organizer.Id, e.Organizer.Name, e.Organizer.Email },
        tickets = e.Tickets.Select(t => new
        {
            t.Id,
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
        }),
        categories = e.CategoryMaps.Select(cm => new
        {
            cm.Category.Id,
            cm.Category.Name,
            cm.Category.Slug,
            cm.Category.Icon
        })
    };
}

/// <summary>
/// Organizer-only endpoints for creating, updating, publishing, and deleting events.
/// </summary>
[ApiController]
[Route("api/organizer/events")]
[Authorize]
public class OrganizerEventsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrganizerEventsController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record CreateEventRequest(
        string Name,
        string? Description,
        string? Format,
        string? Type,
        DateTime? StartDate,
        DateTime? EndDate,
        string? Timezone,
        string? Location,
        string? VirtualLink,
        string? BannerImageUrl,
        string? ThumbnailImageUrl,
        string? RecurringConfig,
        List<int>? CategoryIds);

    public record UpdateEventRequest(
        string? Name,
        string? Description,
        string? Format,
        string? Type,
        DateTime? StartDate,
        DateTime? EndDate,
        string? Timezone,
        string? Location,
        string? VirtualLink,
        string? BannerImageUrl,
        string? ThumbnailImageUrl,
        string? RecurringConfig,
        List<int>? CategoryIds);

    /// <summary>
    /// Lists all events owned by the authenticated organizer.
    /// </summary>
    /// <returns>List of organizer's events</returns>
    /// <response code="200">Events retrieved</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var events = await _db.Events
            .Include(e => e.Tickets)
            .Include(e => e.CategoryMaps).ThenInclude(cm => cm.Category)
            .Where(e => e.OrganizerId == userId.Value)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events.Select(MapOrgEvent)));
    }

    /// <summary>
    /// Creates a new event for the authenticated organizer.
    /// </summary>
    /// <param name="request">Event details</param>
    /// <returns>Created event ID and slug</returns>
    /// <response code="201">Event created</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        // Ensure user has organizer role
        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return Unauthorized(ApiResponse.Fail("User not found"));
        if (user.Role != UserRole.Organizer && user.Role != UserRole.Admin)
        {
            user.Role = UserRole.Organizer;
            user.UpdatedAt = DateTime.UtcNow;
        }

        var slug = GenerateSlug(request.Name);

        var ev = new Event
        {
            OrganizerId = userId.Value,
            Name = request.Name,
            Description = request.Description,
            Slug = slug,
            Format = ParseEnum<EventFormat>(request.Format, EventFormat.Physical),
            Type = ParseEnum<EventType>(request.Type, EventType.Single),
            Status = EventStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone ?? "UTC",
            Location = request.Location,
            VirtualLink = request.VirtualLink,
            BannerImageUrl = request.BannerImageUrl,
            ThumbnailImageUrl = request.ThumbnailImageUrl,
            RecurringConfig = request.RecurringConfig
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        if (request.CategoryIds?.Any() == true)
        {
            foreach (var catId in request.CategoryIds)
            {
                _db.EventCategoryMaps.Add(new EventCategoryMap { EventId = ev.Id, CategoryId = catId });
            }
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetMyEvents), null, ApiResponse<object>.Ok(new { ev.Id, ev.Slug }, "Event created"));
    }

    /// <summary>
    /// Updates an existing event owned by the authenticated organizer.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="request">Fields to update (null fields are skipped)</param>
    /// <returns>Update confirmation</returns>
    /// <response code="200">Event updated</response>
    /// <response code="404">Event not found or not owned by user</response>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.Include(e => e.CategoryMaps).FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        if (request.Name != null) { ev.Name = request.Name; ev.Slug = GenerateSlug(request.Name); }
        if (request.Description != null) ev.Description = request.Description;
        if (request.Format != null) ev.Format = ParseEnum<EventFormat>(request.Format, ev.Format);
        if (request.Type != null) ev.Type = ParseEnum<EventType>(request.Type, ev.Type);
        if (request.StartDate != null) ev.StartDate = request.StartDate;
        if (request.EndDate != null) ev.EndDate = request.EndDate;
        if (request.Timezone != null) ev.Timezone = request.Timezone;
        if (request.Location != null) ev.Location = request.Location;
        if (request.VirtualLink != null) ev.VirtualLink = request.VirtualLink;
        if (request.BannerImageUrl != null) ev.BannerImageUrl = request.BannerImageUrl;
        if (request.ThumbnailImageUrl != null) ev.ThumbnailImageUrl = request.ThumbnailImageUrl;
        if (request.RecurringConfig != null) ev.RecurringConfig = request.RecurringConfig;
        ev.UpdatedAt = DateTime.UtcNow;

        if (request.CategoryIds != null)
        {
            _db.EventCategoryMaps.RemoveRange(ev.CategoryMaps);
            foreach (var catId in request.CategoryIds)
            {
                _db.EventCategoryMaps.Add(new EventCategoryMap { EventId = ev.Id, CategoryId = catId });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Event updated"));
    }

    /// <summary>
    /// Deletes a draft event owned by the authenticated organizer.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Deletion confirmation</returns>
    /// <response code="200">Event deleted</response>
    /// <response code="400">Event is not in draft status</response>
    /// <response code="404">Event not found or not owned by user</response>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));
        if (ev.Status != EventStatus.Draft)
            return BadRequest(ApiResponse.Fail("Only draft events can be deleted"));

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Event deleted"));
    }

    /// <summary>
    /// Publishes a draft event, making it visible to the public.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Publish confirmation</returns>
    /// <response code="200">Event published</response>
    /// <response code="400">Event is already published</response>
    /// <response code="404">Event not found or not owned by user</response>
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));
        if (ev.Status == EventStatus.Published)
            return BadRequest(ApiResponse.Fail("Event is already published"));

        ev.Status = EventStatus.Published;
        ev.PublishedAt = DateTime.UtcNow;
        ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Event published"));
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
        // Remove non-alphanumeric except hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
        return $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private static T ParseEnum<T>(string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }

    private static object MapOrgEvent(Event e) => new
    {
        e.Id,
        e.Name,
        e.Description,
        e.Slug,
        format = e.Format.ToString().ToLowerInvariant(),
        type = e.Type.ToString().ToLowerInvariant(),
        status = e.Status.ToString().ToLowerInvariant(),
        e.IsFeatured,
        e.StartDate,
        e.EndDate,
        e.Timezone,
        e.Location,
        e.VirtualLink,
        e.BannerImageUrl,
        e.ThumbnailImageUrl,
        e.TicketsSold,
        e.TotalRevenue,
        e.CreatedAt,
        e.PublishedAt,
        tickets = e.Tickets.Select(t => new
        {
            t.Id,
            t.Name,
            type = t.Type.ToString().ToLowerInvariant(),
            t.Quantity,
            t.QuantitySold,
            t.Price
        }),
        categories = e.CategoryMaps.Select(cm => new
        {
            cm.Category.Id,
            cm.Category.Name,
            cm.Category.Slug
        })
    };
}
