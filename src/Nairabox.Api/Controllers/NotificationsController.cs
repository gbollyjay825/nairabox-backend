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
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;

    public NotificationsController(NairaboxDbContext db, ICurrentUserService currentUser, IEmailService email)
    {
        _db = db;
        _currentUser = currentUser;
        _email = email;
    }

    public record SendBulkEmailRequest(string Subject, string Content);
    public record ScheduleRemindersRequest(bool Reminder3Days, bool ReminderDayOf);

    [HttpPost("event/{eventId:int}/email")]
    public async Task<IActionResult> SendBulkEmail(int eventId, [FromBody] SendBulkEmailRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var bookings = await _db.Bookings
            .Where(b => b.EventId == eventId && b.PaymentStatus == PaymentStatus.Completed)
            .ToListAsync();

        var emails = bookings.Select(b => b.CustomerEmail).Distinct().ToList();

        if (!emails.Any())
            return BadRequest(ApiResponse.Fail("No attendees to email"));

        // Record the email
        var orgEmail = new OrganizerEmail
        {
            EventId = eventId,
            OrganizerId = userId.Value,
            Subject = request.Subject,
            Content = request.Content,
            RecipientCount = emails.Count,
            SentAt = DateTime.UtcNow
        };
        _db.OrganizerEmails.Add(orgEmail);

        // Create notification records
        foreach (var email in emails)
        {
            _db.EmailNotifications.Add(new EmailNotification
            {
                EventId = eventId,
                RecipientEmail = email,
                Type = NotificationType.OrganizerBulk,
                Subject = request.Subject,
                Content = request.Content,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await _email.SendBulkEmailAsync(emails, request.Subject, request.Content);

        return Ok(ApiResponse<object>.Ok(new { recipientCount = emails.Count }, "Emails sent"));
    }

    [HttpPost("event/{eventId:int}/reminders")]
    public async Task<IActionResult> ScheduleReminders(int eventId, [FromBody] ScheduleRemindersRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        if (ev.StartDate == null)
            return BadRequest(ApiResponse.Fail("Event has no start date"));

        var bookings = await _db.Bookings
            .Where(b => b.EventId == eventId && b.PaymentStatus == PaymentStatus.Completed)
            .ToListAsync();

        var emails = bookings.Select(b => b.CustomerEmail).Distinct().ToList();
        var count = 0;

        foreach (var email in emails)
        {
            if (request.Reminder3Days)
            {
                _db.EmailNotifications.Add(new EmailNotification
                {
                    EventId = eventId,
                    RecipientEmail = email,
                    Type = NotificationType.Reminder3Days,
                    Subject = $"Reminder: {ev.Name} is in 3 days!",
                    Content = $"Don't forget! {ev.Name} starts on {ev.StartDate:g}.",
                    Status = NotificationStatus.Pending
                });
                count++;
            }

            if (request.ReminderDayOf)
            {
                _db.EmailNotifications.Add(new EmailNotification
                {
                    EventId = eventId,
                    RecipientEmail = email,
                    Type = NotificationType.ReminderDayOf,
                    Subject = $"Today: {ev.Name}",
                    Content = $"{ev.Name} is happening today! See you there.",
                    Status = NotificationStatus.Pending
                });
                count++;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { scheduledCount = count }, "Reminders scheduled"));
    }
}
