namespace Nairabox.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp);
    Task SendBookingConfirmationAsync(string email, string bookingId, string eventName);
    Task SendBulkEmailAsync(IEnumerable<string> emails, string subject, string content);
}
