using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;
using QRCoder;

namespace Nairabox.Api.Controllers;

/// <summary>
/// Manages event bookings, payment confirmation, and ticket QR codes.
/// </summary>
[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly IPaymentService _payment;
    private readonly IEmailService _email;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public BookingsController(NairaboxDbContext db, IPaymentService payment, IEmailService email, ICurrentUserService currentUser, IConfiguration config)
    {
        _db = db;
        _payment = payment;
        _email = email;
        _currentUser = currentUser;
        _config = config;
    }

    public record CreateBookingRequest(
        int EventId,
        int TicketId,
        int Quantity,
        string CustomerEmail,
        string CustomerName,
        string? CustomerPhone,
        string? DiscountCode,
        string? PaymentMethod,
        List<AttendeeInfo>? Attendees);

    public record AttendeeInfo(string Name, string Email);

    public record ConfirmPaymentRequest(string PaymentReference);

    /// <summary>
    /// Creates a new booking for an event, applying discounts and service fees.
    /// </summary>
    /// <param name="request">Booking details including event, ticket, quantity, and customer info</param>
    /// <returns>Booking confirmation with payment URL if applicable</returns>
    /// <response code="201">Booking created successfully</response>
    /// <response code="400">Validation error or insufficient ticket availability</response>
    /// <response code="404">Ticket not found</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest(ApiResponse.Fail("Customer email is required"));
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(ApiResponse.Fail("Customer name is required"));
        if (request.Quantity <= 0)
            return BadRequest(ApiResponse.Fail("Quantity must be greater than zero"));

        var ticket = await _db.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == request.TicketId);
        if (ticket == null) return NotFound(ApiResponse.Fail("Ticket not found"));
        if (ticket.EventId != request.EventId) return BadRequest(ApiResponse.Fail("Ticket does not belong to this event"));

        var available = ticket.Quantity - ticket.QuantitySold;
        if (request.Quantity > available)
            return BadRequest(ApiResponse.Fail($"Only {available} tickets available"));

        if (ticket.PurchaseLimit.HasValue && request.Quantity > ticket.PurchaseLimit.Value)
            return BadRequest(ApiResponse.Fail($"Purchase limit is {ticket.PurchaseLimit.Value}"));

        decimal totalAmount = ticket.Price * request.Quantity;
        decimal discountAmount = 0;
        int? discountCodeId = null;

        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d =>
                d.Code == request.DiscountCode && d.IsActive &&
                d.StartDate <= DateTime.UtcNow && d.EndDate >= DateTime.UtcNow);

            if (discount != null && (discount.UsageLimit == null || discount.UsageCount < discount.UsageLimit))
            {
                discountAmount = discount.DiscountType == DiscountType.Percentage
                    ? totalAmount * (discount.DiscountValue / 100)
                    : discount.DiscountValue;

                discountAmount = Math.Min(discountAmount, totalAmount);
                discountCodeId = discount.Id;
                discount.UsageCount++;
            }
        }

        var serviceFeePercentage = _config.GetValue("AppSettings:ServiceFeePercentage", 0.035m);
        decimal serviceFee = ticket.TransferFeesToGuest ? totalAmount * serviceFeePercentage : 0;
        decimal finalAmount = totalAmount - discountAmount + serviceFee;

        var bookingId = $"NB-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var booking = new Booking
        {
            BookingId = bookingId,
            EventId = request.EventId,
            CustomerId = _currentUser.UserId,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            TotalAmount = finalAmount,
            ServiceFee = serviceFee,
            DiscountAmount = discountAmount,
            DiscountCodeId = discountCodeId,
            PaymentStatus = ticket.Type == TicketType.Free ? PaymentStatus.Completed : PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            TicketQuantity = request.Quantity,
            Attendees = request.Attendees != null
                ? System.Text.Json.JsonSerializer.Serialize(request.Attendees)
                : null
        };

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, ApiResponse.Fail("Failed to create booking. Please try again."));
        }

        // Issue tickets
        var attendees = request.Attendees ?? new List<AttendeeInfo>
        {
            new(request.CustomerName, request.CustomerEmail)
        };

        for (int i = 0; i < request.Quantity; i++)
        {
            var attendee = i < attendees.Count ? attendees[i] : attendees[0];
            var issuedTicket = new IssuedTicket
            {
                TicketId = request.TicketId,
                BookingId = booking.Id,
                QrCode = $"{bookingId}-{i + 1}-{Guid.NewGuid().ToString("N")[..8]}",
                AttendeeName = attendee.Name,
                AttendeeEmail = attendee.Email
            };
            _db.IssuedTickets.Add(issuedTicket);
        }

        if (ticket.Type == TicketType.Free)
        {
            ticket.QuantitySold += request.Quantity;
            ticket.Event.TicketsSold += request.Quantity;
        }

        await _db.SaveChangesAsync();

        PaymentResult? paymentResult = null;
        if (ticket.Type != TicketType.Free && finalAmount > 0)
        {
            paymentResult = await _payment.InitializePaymentAsync(finalAmount, request.CustomerEmail, bookingId);
        }

        if (ticket.Type == TicketType.Free)
        {
            await _email.SendBookingConfirmationAsync(request.CustomerEmail, bookingId, ticket.Event.Name);
        }

        return CreatedAtAction(nameof(GetByRef), new { @ref = bookingId }, ApiResponse<object>.Ok(new
        {
            booking.Id,
            booking.BookingId,
            booking.TotalAmount,
            booking.ServiceFee,
            booking.DiscountAmount,
            paymentStatus = booking.PaymentStatus.ToString().ToLowerInvariant(),
            paymentUrl = paymentResult?.AuthorizationUrl
        }, "Booking created"));
    }

    /// <summary>
    /// Retrieves a booking by its reference ID.
    /// </summary>
    /// <param name="ref">Booking reference string (e.g. NB-XXXXXXXX)</param>
    /// <returns>Booking details including issued tickets</returns>
    /// <response code="200">Booking found</response>
    /// <response code="404">Booking not found</response>
    [HttpGet("by-ref/{ref}")]
    public async Task<IActionResult> GetByRef(string @ref)
    {
        var booking = await _db.Bookings
            .Include(b => b.Event)
            .Include(b => b.IssuedTickets).ThenInclude(it => it.Ticket)
            .FirstOrDefaultAsync(b => b.BookingId == @ref);

        if (booking == null) return NotFound(ApiResponse.Fail("Booking not found"));

        return Ok(ApiResponse<object>.Ok(MapBooking(booking)));
    }

    /// <summary>
    /// Retrieves a booking by its numeric ID.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details including issued tickets</returns>
    /// <response code="200">Booking found</response>
    /// <response code="404">Booking not found</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Event)
            .Include(b => b.IssuedTickets).ThenInclude(it => it.Ticket)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound(ApiResponse.Fail("Booking not found"));

        return Ok(ApiResponse<object>.Ok(MapBooking(booking)));
    }

    /// <summary>
    /// Confirms payment for a booking and updates ticket/event statistics.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Payment reference from the payment provider</param>
    /// <returns>Payment confirmation status</returns>
    /// <response code="200">Payment confirmed</response>
    /// <response code="400">Payment already confirmed or verification failed</response>
    /// <response code="404">Booking not found</response>
    [HttpPost("{id:int}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id, [FromBody] ConfirmPaymentRequest request)
    {
        var booking = await _db.Bookings
            .Include(b => b.IssuedTickets).ThenInclude(it => it.Ticket)
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound(ApiResponse.Fail("Booking not found"));
        if (booking.PaymentStatus == PaymentStatus.Completed)
            return BadRequest(ApiResponse.Fail("Payment already confirmed"));

        var result = await _payment.VerifyPaymentAsync(request.PaymentReference);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Message ?? "Payment verification failed"));

        booking.PaymentStatus = PaymentStatus.Completed;
        booking.PaymentReference = request.PaymentReference;
        booking.UpdatedAt = DateTime.UtcNow;

        // Update ticket and event stats
        foreach (var issuedTicket in booking.IssuedTickets)
        {
            issuedTicket.Ticket.QuantitySold++;
        }
        booking.Event.TicketsSold += booking.TicketQuantity;
        booking.Event.TotalRevenue += booking.TotalAmount;

        await _db.SaveChangesAsync();
        await _email.SendBookingConfirmationAsync(booking.CustomerEmail, booking.BookingId, booking.Event.Name);

        return Ok(ApiResponse.Ok("Payment confirmed"));
    }

    /// <summary>
    /// Generates a QR code image for a specific issued ticket.
    /// </summary>
    /// <param name="bookingId">Booking reference string</param>
    /// <param name="ticketId">Issued ticket ID</param>
    /// <returns>PNG image of the QR code</returns>
    /// <response code="200">QR code image returned</response>
    /// <response code="404">Ticket not found</response>
    [HttpGet("{bookingId}/tickets/{ticketId:int}/qrcode")]
    public async Task<IActionResult> GetTicketQrCode(string bookingId, int ticketId)
    {
        var ticket = await _db.IssuedTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return NotFound(ApiResponse.Fail("Ticket not found"));

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(ticket.QrCode, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(10);

        return File(qrCodeImage, "image/png", $"ticket-{ticket.QrCode}.png");
    }

    /// <summary>Get all bookings for a specific event (organizer)</summary>
    [HttpGet("by-event/{eventId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var bookings = await _db.Bookings
            .Where(b => b.EventId == eventId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<Booking>>.Ok(bookings));
    }

    /// <summary>Get current user's bookings</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return Unauthorized(ApiResponse.Fail("User not found"));

        var bookings = await _db.Bookings
            .Where(b => b.CustomerEmail == user.Email)
            .Include(b => b.IssuedTickets)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<Booking>>.Ok(bookings));
    }

    /// <summary>Cancel a pending booking</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound(ApiResponse.Fail("Booking not found"));
        if (booking.PaymentStatus != PaymentStatus.Pending)
            return BadRequest(ApiResponse.Fail("Only pending bookings can be cancelled"));

        booking.PaymentStatus = PaymentStatus.Refunded;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Booking cancelled"));
    }

    private static object MapBooking(Booking b) => new
    {
        b.Id,
        b.BookingId,
        b.EventId,
        eventName = b.Event.Name,
        b.CustomerEmail,
        b.CustomerName,
        b.CustomerPhone,
        b.TotalAmount,
        b.ServiceFee,
        b.DiscountAmount,
        paymentStatus = b.PaymentStatus.ToString().ToLowerInvariant(),
        b.PaymentMethod,
        b.PaymentReference,
        b.TicketQuantity,
        b.IsCheckedIn,
        b.CheckedInAt,
        b.CreatedAt,
        tickets = b.IssuedTickets.Select(it => new
        {
            it.Id,
            it.QrCode,
            it.AttendeeName,
            it.AttendeeEmail,
            it.IsCheckedIn,
            it.CheckedInAt,
            ticketName = it.Ticket.Name,
            ticketType = it.Ticket.Type.ToString().ToLowerInvariant()
        })
    };
}
