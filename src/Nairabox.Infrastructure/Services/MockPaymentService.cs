using Nairabox.Application.Common.Interfaces;

namespace Nairabox.Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    public Task<PaymentResult> InitializePaymentAsync(decimal amount, string email, string reference)
    {
        Console.WriteLine($"[MockPayment] Initialized payment: {amount:N2} for {email}, ref: {reference}");
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            Reference = reference,
            AuthorizationUrl = $"https://mock-payment.nairabox.com/pay/{reference}",
            Message = "Payment initialized (mock)"
        });
    }

    public Task<PaymentResult> VerifyPaymentAsync(string reference)
    {
        Console.WriteLine($"[MockPayment] Verified payment: {reference}");
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            Reference = reference,
            Message = "Payment verified (mock)"
        });
    }
}
