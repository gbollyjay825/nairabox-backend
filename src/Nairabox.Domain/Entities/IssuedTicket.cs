namespace Nairabox.Domain.Entities;

public class IssuedTicket
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int BookingId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int? CheckedInBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Ticket Ticket { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
}
