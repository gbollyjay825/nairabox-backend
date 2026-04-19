using Nairabox.Domain.Entities;

namespace Nairabox.Application.Common.Interfaces;

public interface IAuthService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateOtp();
    int? ValidateAccessToken(string token);
}
