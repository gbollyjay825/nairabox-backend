using Nairabox.Domain.Enums;

namespace Nairabox.Domain.Entities;

public class OtpVerification
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public bool IsVerified { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
