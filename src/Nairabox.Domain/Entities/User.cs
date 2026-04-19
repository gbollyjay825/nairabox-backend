using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string OpenId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string? LoginMethod { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSignedIn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public BankAccount? BankAccount { get; set; }
}
