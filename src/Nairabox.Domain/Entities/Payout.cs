using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class Payout
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int OrganizerId { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal TransferFee { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? PaymentReference { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public User Organizer { get; set; } = null!;
}
