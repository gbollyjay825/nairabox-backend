using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    public string BookingId { get; set; } = string.Empty; // Human-readable NB-XXXXXXXX
    public int EventId { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? DiscountCodeId { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public int TicketQuantity { get; set; }
    public string? Attendees { get; set; } // JSON array
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public DiscountCode? DiscountCode { get; set; }
    public ICollection<IssuedTicket> IssuedTickets { get; set; } = new List<IssuedTicket>();
}
