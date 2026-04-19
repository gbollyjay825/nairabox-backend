namespace Nairabox.Domain.Entities;

public class BankAccount
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationDetails { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Organizer { get; set; } = null!;
}
