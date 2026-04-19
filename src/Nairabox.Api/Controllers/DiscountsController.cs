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
[Route("api/discounts")]
public class DiscountsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DiscountsController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record CreateDiscountRequest(
        string Code,
        string DiscountType,
        decimal DiscountValue,
        DateTime StartDate,
        DateTime EndDate,
        int? UsageLimit,
        int? MinTicketQuantity,
        int? MaxTicketQuantity,
        string? ApplicableEvents,
        string? ApplicableTickets);

    public record UpdateDiscountRequest(
        string? Code,
        string? DiscountType,
        decimal? DiscountValue,
        DateTime? StartDate,
        DateTime? EndDate,
        int? UsageLimit,
        int? MinTicketQuantity,
        int? MaxTicketQuantity,
        string? ApplicableEvents,
        string? ApplicableTickets,
        bool? IsActive);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse.Fail("Discount code is required"));
        if (request.DiscountValue <= 0)
            return BadRequest(ApiResponse.Fail("Discount value must be greater than zero"));
        if (request.EndDate <= request.StartDate)
            return BadRequest(ApiResponse.Fail("End date must be after start date"));

        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var discount = new DiscountCode
        {
            OrganizerId = userId.Value,
            Code = request.Code.ToUpperInvariant(),
            DiscountType = Enum.TryParse<DiscountType>(request.DiscountType, true, out var dt) ? dt : DiscountType.Fixed,
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            MinTicketQuantity = request.MinTicketQuantity,
            MaxTicketQuantity = request.MaxTicketQuantity,
            ApplicableEvents = request.ApplicableEvents,
            ApplicableTickets = request.ApplicableTickets
        };

        _db.DiscountCodes.Add(discount);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganizerDiscounts), null,
            ApiResponse<object>.Ok(new { discount.Id, discount.Code }, "Discount created"));
    }

    [Authorize]
    [HttpGet("organizer")]
    public async Task<IActionResult> GetOrganizerDiscounts()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var discounts = await _db.DiscountCodes
            .Where(d => d.OrganizerId == userId.Value)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(discounts.Select(MapDiscount)));
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string code, [FromQuery] int? eventId)
    {
        var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d =>
            d.Code == code.ToUpperInvariant() && d.IsActive &&
            d.StartDate <= DateTime.UtcNow && d.EndDate >= DateTime.UtcNow);

        if (discount == null)
            return NotFound(ApiResponse.Fail("Invalid or expired discount code"));

        if (discount.UsageLimit.HasValue && discount.UsageCount >= discount.UsageLimit.Value)
            return BadRequest(ApiResponse.Fail("Discount code usage limit reached"));

        return Ok(ApiResponse<object>.Ok(new
        {
            discount.Code,
            discountType = discount.DiscountType.ToString().ToLowerInvariant(),
            discount.DiscountValue,
            discount.MinTicketQuantity,
            discount.MaxTicketQuantity
        }));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDiscountRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id && d.OrganizerId == userId.Value);
        if (discount == null) return NotFound(ApiResponse.Fail("Discount not found"));

        if (request.Code != null) discount.Code = request.Code.ToUpperInvariant();
        if (request.DiscountType != null && Enum.TryParse<DiscountType>(request.DiscountType, true, out var dt))
            discount.DiscountType = dt;
        if (request.DiscountValue.HasValue) discount.DiscountValue = request.DiscountValue.Value;
        if (request.StartDate.HasValue) discount.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) discount.EndDate = request.EndDate.Value;
        if (request.UsageLimit.HasValue) discount.UsageLimit = request.UsageLimit.Value;
        if (request.MinTicketQuantity.HasValue) discount.MinTicketQuantity = request.MinTicketQuantity.Value;
        if (request.MaxTicketQuantity.HasValue) discount.MaxTicketQuantity = request.MaxTicketQuantity.Value;
        if (request.ApplicableEvents != null) discount.ApplicableEvents = request.ApplicableEvents;
        if (request.ApplicableTickets != null) discount.ApplicableTickets = request.ApplicableTickets;
        if (request.IsActive.HasValue) discount.IsActive = request.IsActive.Value;
        discount.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Discount updated"));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id && d.OrganizerId == userId.Value);
        if (discount == null) return NotFound(ApiResponse.Fail("Discount not found"));

        _db.DiscountCodes.Remove(discount);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Discount deleted"));
    }

    private static object MapDiscount(DiscountCode d) => new
    {
        d.Id,
        d.Code,
        discountType = d.DiscountType.ToString().ToLowerInvariant(),
        d.DiscountValue,
        d.StartDate,
        d.EndDate,
        d.UsageLimit,
        d.UsageCount,
        d.MinTicketQuantity,
        d.MaxTicketQuantity,
        d.ApplicableEvents,
        d.ApplicableTickets,
        d.IsActive,
        d.CreatedAt
    };
}
