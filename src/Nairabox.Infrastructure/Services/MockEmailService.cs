using Nairabox.Application.Common.Interfaces;

namespace Nairabox.Infrastructure.Services;

public class MockEmailService : IEmailService
{
    public Task SendOtpEmailAsync(string email, string otp)
    {
        Console.WriteLine($"[MockEmail] OTP {otp} sent to {email}");
        return Task.CompletedTask;
    }

    public Task SendBookingConfirmationAsync(string email, string bookingId, string eventName)
    {
        Console.WriteLine($"[MockEmail] Booking confirmation {bookingId} for '{eventName}' sent to {email}");
        return Task.CompletedTask;
    }

    public Task SendBulkEmailAsync(IEnumerable<string> emails, string subject, string content)
    {
        var recipients = string.Join(", ", emails);
        Console.WriteLine($"[MockEmail] Bulk email '{subject}' sent to {recipients}");
        return Task.CompletedTask;
    }
}
