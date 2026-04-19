using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public EventFormat Format { get; set; } = EventFormat.Physical;
    public EventType Type { get; set; } = EventType.Single;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public bool IsFeatured { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Timezone { get; set; } = "UTC";
    public string? Location { get; set; } // JSON
    public string? VirtualLink { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public int TicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public string? RecurringConfig { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    // Navigation properties
    public User Organizer { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<EventCategoryMap> CategoryMaps { get; set; } = new List<EventCategoryMap>();
}
