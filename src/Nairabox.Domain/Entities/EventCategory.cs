namespace Nairabox.Domain.Entities;

public class EventCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EventCategoryMap> EventMaps { get; set; } = new List<EventCategoryMap>();
}

public class EventCategoryMap
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    public Event Event { get; set; } = null!;
    public EventCategory Category { get; set; } = null!;
}
