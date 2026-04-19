using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nairabox.Domain.Entities;

namespace Nairabox.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OpenId).HasColumnName("openId").HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.OpenId).IsUnique();
        builder.Property(e => e.Name).HasColumnName("name").HasColumnType("text");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.Property(e => e.FirstName).HasColumnName("firstName").HasMaxLength(255);
        builder.Property(e => e.LastName).HasColumnName("lastName").HasMaxLength(255);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.PasswordHash).HasColumnName("passwordHash").HasColumnType("text");
        builder.Property(e => e.LoginMethod).HasColumnName("loginMethod").HasMaxLength(64);
        builder.Property(e => e.Role).HasColumnName("role")
            .HasConversion<string>()
            .HasDefaultValue(Domain.Enums.UserRole.User);
        builder.Property(e => e.IsEmailVerified).HasColumnName("isEmailVerified").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.Property(e => e.LastSignedIn).HasColumnName("lastSignedIn");
    }
}
