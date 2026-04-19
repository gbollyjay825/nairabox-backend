using Microsoft.EntityFrameworkCore;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api;

public static class SeedData
{
    public static async Task Initialize(NairaboxDbContext db)
    {
        if (await db.Users.AnyAsync()) return; // Already seeded

        // Create demo users
        var organizer = new User
        {
            OpenId = "demo-organizer",
            Email = "demo-organizer@nairabox.com",
            FirstName = "Annie",
            LastName = "Obot",
            Name = "Annie Obot",
            Role = UserRole.Organizer,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123"),
            IsEmailVerified = true,
            LoginMethod = "email"
        };
        var admin = new User
        {
            OpenId = "demo-admin",
            Email = "demo-admin@nairabox.com",
            FirstName = "Admin",
            LastName = "Nairabox",
            Name = "Admin Nairabox",
            Role = UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123"),
            IsEmailVerified = true,
            LoginMethod = "email"
        };
        var user = new User
        {
            OpenId = "demo-user",
            Email = "demo-user@nairabox.com",
            FirstName = "John",
            LastName = "Doe",
            Name = "John Doe",
            Role = UserRole.User,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123"),
            IsEmailVerified = true,
            LoginMethod = "email"
        };

        db.Users.AddRange(organizer, admin, user);
        await db.SaveChangesAsync();

        // Create categories
        var categories = new[]
        {
            new EventCategory { Name = "Concerts", Slug = "concerts", Icon = "music" },
            new EventCategory { Name = "Raves", Slug = "raves", Icon = "headphones" },
            new EventCategory { Name = "Night Life", Slug = "night-life", Icon = "moon" },
            new EventCategory { Name = "Business", Slug = "business", Icon = "briefcase" },
            new EventCategory { Name = "Food & Drinks", Slug = "food-drinks", Icon = "utensils" },
            new EventCategory { Name = "Art & Culture", Slug = "art-culture", Icon = "palette" },
            new EventCategory { Name = "Sport", Slug = "sport", Icon = "football" },
            new EventCategory { Name = "Wellness", Slug = "wellness", Icon = "heart" },
        };
        db.EventCategories.AddRange(categories);
        await db.SaveChangesAsync();

        // Create sample events
        var events = new[]
        {
            new Event
            {
                OrganizerId = organizer.Id,
                Name = "Rema Rocks the City",
                Description = "An unforgettable concert experience featuring Rema live on stage with amazing visuals and sound.",
                Slug = "rema-rocks-the-city-1",
                Format = EventFormat.Physical,
                Status = EventStatus.Published,
                IsFeatured = true,
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(7).AddHours(5),
                Location = "{\"address\":\"Eko Energy World, Lagos\"}",
                BannerImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=1200&h=500&fit=crop",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=600&h=400&fit=crop",
                PublishedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Event
            {
                OrganizerId = organizer.Id,
                Name = "Burna Boy Live in Concert",
                Description = "The African Giant live on stage - a night you will never forget.",
                Slug = "burna-boy-live-1",
                Format = EventFormat.Physical,
                Status = EventStatus.Published,
                IsFeatured = true,
                StartDate = DateTime.UtcNow.AddDays(14),
                EndDate = DateTime.UtcNow.AddDays(14).AddHours(4),
                Location = "{\"address\":\"Landmark Event Centre, Lagos\"}",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=600&h=400&fit=crop",
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Event
            {
                OrganizerId = organizer.Id,
                Name = "Afrobeats Summer Fest",
                Description = "Three days of non-stop Afrobeats featuring top artists from across Africa.",
                Slug = "afrobeats-fest-1",
                Format = EventFormat.Physical,
                Status = EventStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(21),
                EndDate = DateTime.UtcNow.AddDays(23),
                Location = "{\"address\":\"Muri Okunola Park, Lagos\"}",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?w=600&h=400&fit=crop",
                PublishedAt = DateTime.UtcNow.AddDays(-1)
            },
        };
        db.Events.AddRange(events);
        await db.SaveChangesAsync();

        // Create tickets for first event
        var tickets = new[]
        {
            new Ticket
            {
                EventId = events[0].Id,
                Name = "Regular Ticket",
                Type = TicketType.Paid,
                Quantity = 500,
                Price = 50000,
                PurchaseLimit = 5
            },
            new Ticket
            {
                EventId = events[0].Id,
                Name = "VIP Ticket",
                Type = TicketType.Paid,
                Quantity = 100,
                Price = 100000,
                PurchaseLimit = 2
            },
            new Ticket
            {
                EventId = events[0].Id,
                Name = "VVIP Ticket",
                Type = TicketType.Paid,
                Quantity = 30,
                Price = 200000,
                PurchaseLimit = 1
            },
        };
        db.Tickets.AddRange(tickets);

        // Create tickets for second event
        db.Tickets.AddRange(
            new Ticket { EventId = events[1].Id, Name = "General Admission", Type = TicketType.Paid, Quantity = 1000, Price = 35000, PurchaseLimit = 5 },
            new Ticket { EventId = events[1].Id, Name = "VIP", Type = TicketType.Paid, Quantity = 200, Price = 75000, PurchaseLimit = 3 }
        );

        // Create tickets for third event
        db.Tickets.AddRange(
            new Ticket { EventId = events[2].Id, Name = "Day Pass", Type = TicketType.Paid, Quantity = 2000, Price = 25000, PurchaseLimit = 5 },
            new Ticket { EventId = events[2].Id, Name = "3-Day Pass", Type = TicketType.Paid, Quantity = 500, Price = 60000, PurchaseLimit = 2 }
        );

        await db.SaveChangesAsync();

        // Link events to categories
        db.EventCategoryMaps.AddRange(
            new EventCategoryMap { EventId = events[0].Id, CategoryId = categories[0].Id }, // Concerts
            new EventCategoryMap { EventId = events[1].Id, CategoryId = categories[0].Id }, // Concerts
            new EventCategoryMap { EventId = events[2].Id, CategoryId = categories[0].Id }, // Concerts
            new EventCategoryMap { EventId = events[2].Id, CategoryId = categories[1].Id }  // Raves
        );
        await db.SaveChangesAsync();
    }
}
