using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketType Type { get; set; } = TicketType.Free;
    public int Quantity { get; set; }
    public int QuantitySold { get; set; }
    public decimal Price { get; set; }
    public int? PurchaseLimit { get; set; }
    public DateTime? SalesStartDate { get; set; }
    public DateTime? SalesEndDate { get; set; }
    public bool IsRefundable { get; set; } = true;
    public bool TransferFeesToGuest { get; set; }
    public bool IsInviteOnly { get; set; }
    public string? InviteOnlyPassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public ICollection<IssuedTicket> IssuedTickets { get; set; } = new List<IssuedTicket>();
}
