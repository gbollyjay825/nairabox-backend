using Microsoft.EntityFrameworkCore;
using Nairabox.Domain.Entities;

namespace Nairabox.Infrastructure.Data;

public class NairaboxDbContext : DbContext
{
    public NairaboxDbContext(DbContextOptions<NairaboxDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<EventCategoryMap> EventCategoryMaps => Set<EventCategoryMap>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<IssuedTicket> IssuedTickets => Set<IssuedTicket>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();
    public DbSet<OrganizerEmail> OrganizerEmails => Set<OrganizerEmail>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<TicketHold> TicketHolds => Set<TicketHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NairaboxDbContext).Assembly);
    }
}
