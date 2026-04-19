using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nairabox.Domain.Entities;

namespace Nairabox.Infrastructure.Data.Configurations;

public class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
{
    public void Configure(EntityTypeBuilder<EventCategory> builder)
    {
        builder.ToTable("eventCategories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique();
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(255);
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
    }
}

public class EventCategoryMapConfiguration : IEntityTypeConfiguration<EventCategoryMap>
{
    public void Configure(EntityTypeBuilder<EventCategoryMap> builder)
    {
        builder.ToTable("eventCategoryMap");
        builder.HasKey(e => new { e.EventId, e.CategoryId });
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.CategoryId).HasColumnName("categoryId");
        builder.HasOne(e => e.Event).WithMany(ev => ev.CategoryMaps).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Category).WithMany(c => c.EventMaps).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasDefaultValue(Domain.Enums.TicketType.Free);
        builder.Property(e => e.Quantity).HasColumnName("quantity");
        builder.Property(e => e.QuantitySold).HasColumnName("quantitySold").HasDefaultValue(0);
        builder.Property(e => e.Price).HasColumnName("price").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.PurchaseLimit).HasColumnName("purchaseLimit");
        builder.Property(e => e.SalesStartDate).HasColumnName("salesStartDate");
        builder.Property(e => e.SalesEndDate).HasColumnName("salesEndDate");
        builder.Property(e => e.IsRefundable).HasColumnName("isRefundable").HasDefaultValue(true);
        builder.Property(e => e.TransferFeesToGuest).HasColumnName("transferFeesToGuest").HasDefaultValue(false);
        builder.Property(e => e.IsInviteOnly).HasColumnName("isInviteOnly").HasDefaultValue(false);
        builder.Property(e => e.InviteOnlyPassword).HasColumnName("inviteOnlyPassword").HasMaxLength(255);
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasIndex(e => e.EventId).HasDatabaseName("eventId_idx");
        builder.HasOne(e => e.Event).WithMany(ev => ev.Tickets).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bankAccounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.HasIndex(e => e.OrganizerId).IsUnique();
        builder.Property(e => e.BankCode).HasColumnName("bankCode").HasMaxLength(10).IsRequired();
        builder.Property(e => e.BankName).HasColumnName("bankName").HasMaxLength(255).IsRequired();
        builder.Property(e => e.AccountNumber).HasColumnName("accountNumber").HasMaxLength(20).IsRequired();
        builder.Property(e => e.AccountName).HasColumnName("accountName").HasMaxLength(255).IsRequired();
        builder.Property(e => e.IsVerified).HasColumnName("isVerified").HasDefaultValue(false);
        builder.Property(e => e.VerificationDetails).HasColumnName("verificationDetails").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasOne(e => e.Organizer).WithOne(u => u.BankAccount).HasForeignKey<BankAccount>(e => e.OrganizerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.BookingId).HasColumnName("bookingId").HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.BookingId).IsUnique();
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.CustomerId).HasColumnName("customerId");
        builder.Property(e => e.CustomerEmail).HasColumnName("customerEmail").HasMaxLength(320).IsRequired();
        builder.Property(e => e.CustomerName).HasColumnName("customerName").HasMaxLength(255).IsRequired();
        builder.Property(e => e.CustomerPhone).HasColumnName("customerPhone").HasMaxLength(20);
        builder.Property(e => e.TotalAmount).HasColumnName("totalAmount").HasPrecision(15, 2);
        builder.Property(e => e.ServiceFee).HasColumnName("serviceFee").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.DiscountAmount).HasColumnName("discountAmount").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.DiscountCodeId).HasColumnName("discountCodeId");
        builder.Property(e => e.PaymentStatus).HasColumnName("paymentStatus").HasConversion<string>().HasDefaultValue(Domain.Enums.PaymentStatus.Pending);
        builder.Property(e => e.PaymentMethod).HasColumnName("paymentMethod").HasMaxLength(50);
        builder.Property(e => e.PaymentReference).HasColumnName("paymentReference").HasMaxLength(255);
        builder.Property(e => e.TicketQuantity).HasColumnName("ticketQuantity");
        builder.Property(e => e.Attendees).HasColumnName("attendees").HasColumnType("text");
        builder.Property(e => e.IsCheckedIn).HasColumnName("isCheckedIn").HasDefaultValue(false);
        builder.Property(e => e.CheckedInAt).HasColumnName("checkedInAt");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasIndex(e => e.EventId).HasDatabaseName("eventId_idx");
        builder.HasIndex(e => e.CustomerEmail).HasDatabaseName("customerEmail_idx");
        builder.HasIndex(e => e.PaymentStatus).HasDatabaseName("paymentStatus_idx");
        builder.HasOne(e => e.Event).WithMany(ev => ev.Bookings).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.DiscountCode).WithMany().HasForeignKey(e => e.DiscountCodeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class IssuedTicketConfiguration : IEntityTypeConfiguration<IssuedTicket>
{
    public void Configure(EntityTypeBuilder<IssuedTicket> builder)
    {
        builder.ToTable("issuedTickets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TicketId).HasColumnName("ticketId");
        builder.Property(e => e.BookingId).HasColumnName("bookingId");
        builder.Property(e => e.QrCode).HasColumnName("qrCode").HasMaxLength(500).IsRequired();
        builder.HasIndex(e => e.QrCode).IsUnique();
        builder.Property(e => e.AttendeeName).HasColumnName("attendeeName").HasMaxLength(255).IsRequired();
        builder.Property(e => e.AttendeeEmail).HasColumnName("attendeeEmail").HasMaxLength(320).IsRequired();
        builder.Property(e => e.IsCheckedIn).HasColumnName("isCheckedIn").HasDefaultValue(false);
        builder.Property(e => e.CheckedInAt).HasColumnName("checkedInAt");
        builder.Property(e => e.CheckedInBy).HasColumnName("checkedInBy");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.HasIndex(e => e.TicketId).HasDatabaseName("ticketId_idx");
        builder.HasIndex(e => e.BookingId).HasDatabaseName("bookingId_idx");
        builder.HasIndex(e => e.QrCode).HasDatabaseName("qrCode_idx");
        builder.HasOne(e => e.Ticket).WithMany(t => t.IssuedTickets).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Booking).WithMany(b => b.IssuedTickets).HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.ToTable("discountCodes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DiscountType).HasColumnName("discountType").HasConversion<string>();
        builder.Property(e => e.DiscountValue).HasColumnName("discountValue").HasPrecision(15, 2);
        builder.Property(e => e.StartDate).HasColumnName("startDate");
        builder.Property(e => e.EndDate).HasColumnName("endDate");
        builder.Property(e => e.UsageLimit).HasColumnName("usageLimit");
        builder.Property(e => e.UsageCount).HasColumnName("usageCount").HasDefaultValue(0);
        builder.Property(e => e.MinTicketQuantity).HasColumnName("minTicketQuantity");
        builder.Property(e => e.MaxTicketQuantity).HasColumnName("maxTicketQuantity");
        builder.Property(e => e.ApplicableEvents).HasColumnName("applicableEvents").HasColumnType("text");
        builder.Property(e => e.ApplicableTickets).HasColumnName("applicableTickets").HasColumnType("text");
        builder.Property(e => e.IsActive).HasColumnName("isActive").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("organizerId_idx");
        builder.HasIndex(e => e.Code).HasDatabaseName("code_idx");
    }
}

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.Property(e => e.TotalRevenue).HasColumnName("totalRevenue").HasPrecision(15, 2);
        builder.Property(e => e.ServiceFee).HasColumnName("serviceFee").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.PayoutAmount).HasColumnName("payoutAmount").HasPrecision(15, 2);
        builder.Property(e => e.TransferFee).HasColumnName("transferFee").HasPrecision(15, 2).HasDefaultValue(0m);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasDefaultValue(Domain.Enums.PayoutStatus.Pending);
        builder.Property(e => e.PaymentReference).HasColumnName("paymentReference").HasMaxLength(255);
        builder.Property(e => e.TransactionId).HasColumnName("transactionId").HasMaxLength(255);
        builder.Property(e => e.ScheduledDate).HasColumnName("scheduledDate");
        builder.Property(e => e.PaidDate).HasColumnName("paidDate");
        builder.Property(e => e.FailureReason).HasColumnName("failureReason").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasIndex(e => e.EventId).HasDatabaseName("eventId_idx");
        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("organizerId_idx");
        builder.HasIndex(e => e.Status).HasDatabaseName("status_idx");
    }
}

public class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotification>
{
    public void Configure(EntityTypeBuilder<EmailNotification> builder)
    {
        builder.ToTable("emailNotifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.BookingId).HasColumnName("bookingId");
        builder.Property(e => e.RecipientEmail).HasColumnName("recipientEmail").HasMaxLength(320).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content").HasColumnType("text");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasDefaultValue(Domain.Enums.NotificationStatus.Pending);
        builder.Property(e => e.SentAt).HasColumnName("sentAt");
        builder.Property(e => e.FailureReason).HasColumnName("failureReason").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.HasIndex(e => e.EventId).HasDatabaseName("eventId_idx");
        builder.HasIndex(e => e.RecipientEmail).HasDatabaseName("recipientEmail_idx");
        builder.HasIndex(e => e.Type).HasDatabaseName("type_idx");
    }
}

public class OrganizerEmailConfiguration : IEntityTypeConfiguration<OrganizerEmail>
{
    public void Configure(EntityTypeBuilder<OrganizerEmail> builder)
    {
        builder.ToTable("organizerEmails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.HasIndex(e => e.EventId).IsUnique();
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(e => e.RecipientCount).HasColumnName("recipientCount").HasDefaultValue(0);
        builder.Property(e => e.SentAt).HasColumnName("sentAt");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("organizerId_idx");
    }
}

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staffMembers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizerId).HasColumnName("organizerId");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("passwordHash").HasColumnType("text");
        builder.Property(e => e.IsActive).HasColumnName("isActive").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("organizerId_idx");
        builder.HasIndex(e => e.Email).HasDatabaseName("email_idx");
    }
}

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otpVerifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(e => e.Otp).HasColumnName("otp").HasMaxLength(6).IsRequired();
        builder.Property(e => e.Purpose).HasColumnName("purpose").HasConversion<string>();
        builder.Property(e => e.IsVerified).HasColumnName("isVerified").HasDefaultValue(false);
        builder.Property(e => e.ExpiresAt).HasColumnName("expiresAt");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.HasIndex(e => e.Email).HasDatabaseName("email_idx");
    }
}

public class TicketHoldConfiguration : IEntityTypeConfiguration<TicketHold>
{
    public void Configure(EntityTypeBuilder<TicketHold> builder)
    {
        builder.ToTable("ticketHolds");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventId).HasColumnName("eventId");
        builder.Property(e => e.TicketId).HasColumnName("ticketId");
        builder.Property(e => e.Quantity).HasColumnName("quantity");
        builder.Property(e => e.SessionId).HasColumnName("sessionId").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expiresAt");
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");
        builder.HasIndex(e => e.EventId).HasDatabaseName("eventId_idx");
        builder.HasIndex(e => e.SessionId).HasDatabaseName("sessionId_idx");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("expiresAt_idx");
    }
}
