using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class DiscountCode
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public int? MinTicketQuantity { get; set; }
    public int? MaxTicketQuantity { get; set; }
    public string? ApplicableEvents { get; set; } // JSON
    public string? ApplicableTickets { get; set; } // JSON
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Organizer { get; set; } = null!;
}
