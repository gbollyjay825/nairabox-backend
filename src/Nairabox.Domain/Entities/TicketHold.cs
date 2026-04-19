namespace Nairabox.Domain.Entities;

/// <summary>
/// Ticket hold entity for checkout session reservation.
/// TODO: Implement ticket hold logic in BookingsController for overselling prevention.
/// </summary>
public class TicketHold
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int TicketId { get; set; }
    public int Quantity { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
