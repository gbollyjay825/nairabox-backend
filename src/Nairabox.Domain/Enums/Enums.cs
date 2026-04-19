namespace Nairabox.Domain.Enums;

public enum UserRole
{
    User,
    Organizer,
    Admin
}

public enum EventFormat
{
    Physical,
    Virtual,
    Hybrid
}

public enum EventType
{
    Single,
    Recurring
}

public enum EventStatus
{
    Draft,
    Published,
    Ended,
    Cancelled
}

public enum TicketType
{
    Free,
    Paid,
    InviteOnly
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PayoutStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum DiscountType
{
    Fixed,
    Percentage
}

public enum NotificationType
{
    Reminder3Days,
    ReminderDayOf,
    OrganizerBulk,
    TicketConfirmation
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

public enum OtpPurpose
{
    Signup,
    PasswordReset
}
