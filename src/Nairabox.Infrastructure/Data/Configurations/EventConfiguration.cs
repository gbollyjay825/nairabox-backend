using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nairabox.Domain.Entities;

namespace Nairabox.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Format).HasColumnName("format").HasConversion<string>().HasDefaultValue(Domain.Enums.EventFormat.Physical);
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasDefaultValue(Domain.Enums.EventType.Single);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasDefaultValue(Domain.Enums.EventStatus.Draft);
        builder.Property(e => e.IsFeatured).HasColumnName("isFeatured").HasDefaultValue(false);
        builder.Property(e => e.StartDate).HasColumnName("startDate");
        builder.Property(e => e.EndDate).HasColumnName("endDate");
        builder.Property(e => e.Timezone).HasColumnName("timezone").HasMaxLength(255).HasDefaultValue("UTC");
        builder.Property(e => e.Location).HasColumnName("location").HasColumnType("text");
        builder.Property(e => e.VirtualLink).HasColumnName("virtualLink").HasMaxLength(500);
        builder.Property(e => e.BannerImageUrl).HasColumnName("bannerImageUrl").HasMaxLength(500);
        builder.Property(e => e.ThumbnailImageUrl).HasColumnName("thumbnailImageUrl").HasMaxLength(500);
        builder.Property(e => e.TicketsSold).HasColumnName("ticketsSold").HasDefaultValue(0);
        builder.Property(e => e.TotalRevenue).HasColumnName("totalRevenue").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.RecurringConfig).HasColumnName("recurringConfig").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.Property(e => e.PublishedAt).HasColumnName("publishedAt");

        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("organizerId_idx");
        builder.HasIndex(e => e.Slug).HasDatabaseName("slug_idx");
        builder.HasIndex(e => e.Status).HasDatabaseName("status_idx");

        builder.HasOne(e => e.Organizer).WithMany(u => u.Events).HasForeignKey(e => e.OrganizerId).OnDelete(DeleteBehavior.Restrict);
    }
}
