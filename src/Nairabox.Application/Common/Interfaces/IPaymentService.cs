namespace Nairabox.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> InitializePaymentAsync(decimal amount, string email, string reference);
    Task<PaymentResult> VerifyPaymentAsync(string reference);
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? Reference { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? Message { get; set; }
}
