namespace Nairabox.Domain.Entities;

public class OrganizerEmail
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int OrganizerId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public User Organizer { get; set; } = null!;
}
