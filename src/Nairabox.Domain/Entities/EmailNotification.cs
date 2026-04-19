using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class EmailNotification
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int? BookingId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Content { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
}
