using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

/// <summary>
/// Handles user authentication, registration, and password management.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly IAuthService _auth;
    private readonly IEmailService _email;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public AuthController(NairaboxDbContext db, IAuthService auth, IEmailService email, ICurrentUserService currentUser, IConfiguration config)
    {
        _db = db;
        _auth = auth;
        _email = email;
        _currentUser = currentUser;
        _config = config;
    }

    public record SignInRequest(string Email, string Password);
    public record RequestOtpRequest(string Email, string? Name);
    public record VerifySignupOtpRequest(string Email, string Otp, string Password, string? Name, string? Phone);
    public record PasswordResetOtpRequest(string Email);
    public record VerifyPasswordOtpRequest(string Email, string Otp, string NewPassword);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Sign-in credentials</param>
    /// <returns>JWT access token and user profile</returns>
    /// <response code="200">Successful authentication</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        var minPasswordLength = _config.GetValue("AppSettings:MinPasswordLength", 6);

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(ApiResponse.Fail("A valid email is required"));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < minPasswordLength)
            return BadRequest(ApiResponse.Fail($"Password must be at least {minPasswordLength} characters"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(ApiResponse.Fail("Invalid email or password"));

        if (!_auth.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse.Fail("Invalid email or password"));

        var accessToken = _auth.GenerateAccessToken(user);
        var refreshToken = _auth.GenerateRefreshToken();

        user.LastSignedIn = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        SetRefreshCookie(refreshToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            accessToken,
            user = MapUser(user)
        }));
    }

    /// <summary>
    /// Requests a one-time password for new user registration.
    /// </summary>
    /// <param name="request">Email address to send the OTP to</param>
    /// <returns>Confirmation that OTP was sent</returns>
    /// <response code="200">OTP sent successfully</response>
    /// <response code="400">Email already registered or invalid</response>
    [HttpPost("signup/request-otp")]
    public async Task<IActionResult> RequestSignupOtp([FromBody] RequestOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(ApiResponse.Fail("A valid email is required"));

        var existingUser = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (existingUser)
            return BadRequest(ApiResponse.Fail("Email already registered"));

        var otpExpirationMinutes = _config.GetValue("AppSettings:OtpExpirationMinutes", 10);
        var otp = _auth.GenerateOtp();

        var otpEntity = new OtpVerification
        {
            Email = request.Email,
            Otp = otp,
            Purpose = OtpPurpose.Signup,
            ExpiresAt = DateTime.UtcNow.AddMinutes(otpExpirationMinutes)
        };

        _db.OtpVerifications.Add(otpEntity);
        await _db.SaveChangesAsync();
        await _email.SendOtpEmailAsync(request.Email, otp);

        return Ok(ApiResponse.Ok("OTP sent to email"));
    }

    /// <summary>
    /// Verifies the signup OTP and creates a new user account.
    /// </summary>
    /// <param name="request">OTP code, email, password, and optional profile info</param>
    /// <returns>JWT access token and newly created user profile</returns>
    /// <response code="200">Account created successfully</response>
    /// <response code="400">Invalid or expired OTP, or validation error</response>
    [HttpPost("signup/verify-otp")]
    public async Task<IActionResult> VerifySignupOtp([FromBody] VerifySignupOtpRequest request)
    {
        var minPasswordLength = _config.GetValue("AppSettings:MinPasswordLength", 6);

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(ApiResponse.Fail("A valid email is required"));
        if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Length != 6 || !request.Otp.All(char.IsDigit))
            return BadRequest(ApiResponse.Fail("OTP must be 6 digits"));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < minPasswordLength)
            return BadRequest(ApiResponse.Fail($"Password must be at least {minPasswordLength} characters"));

        var otpRecord = await _db.OtpVerifications
            .Where(o => o.Email == request.Email && o.Purpose == OtpPurpose.Signup && !o.IsVerified)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null || otpRecord.Otp != request.Otp || otpRecord.ExpiresAt < DateTime.UtcNow)
            return BadRequest(ApiResponse.Fail("Invalid or expired OTP"));

        otpRecord.IsVerified = true;

        var user = new User
        {
            OpenId = Guid.NewGuid().ToString("N"),
            Email = request.Email,
            Name = request.Name,
            FirstName = request.Name?.Split(' ').FirstOrDefault(),
            LastName = request.Name?.Split(' ').Skip(1).FirstOrDefault(),
            Phone = request.Phone,
            PasswordHash = _auth.HashPassword(request.Password),
            LoginMethod = "email",
            Role = UserRole.User,
            IsEmailVerified = true
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse.Fail("An account with this email already exists"));
        }

        var accessToken = _auth.GenerateAccessToken(user);
        var refreshToken = _auth.GenerateRefreshToken();
        SetRefreshCookie(refreshToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            accessToken,
            user = MapUser(user)
        }, "Account created successfully"));
    }

    /// <summary>
    /// Requests a one-time password for password reset.
    /// </summary>
    /// <param name="request">Email address for the password reset</param>
    /// <returns>Confirmation message (always succeeds to prevent email enumeration)</returns>
    /// <response code="200">OTP sent if email exists</response>
    [HttpPost("password/request-otp")]
    public async Task<IActionResult> RequestPasswordOtp([FromBody] PasswordResetOtpRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return Ok(ApiResponse.Ok("If the email exists, an OTP has been sent"));

        var otpExpirationMinutes = _config.GetValue("AppSettings:OtpExpirationMinutes", 10);
        var otp = _auth.GenerateOtp();

        var otpEntity = new OtpVerification
        {
            Email = request.Email,
            Otp = otp,
            Purpose = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(otpExpirationMinutes)
        };

        _db.OtpVerifications.Add(otpEntity);
        await _db.SaveChangesAsync();
        await _email.SendOtpEmailAsync(request.Email, otp);

        return Ok(ApiResponse.Ok("If the email exists, an OTP has been sent"));
    }

    /// <summary>
    /// Verifies OTP and resets the user's password.
    /// </summary>
    /// <param name="request">OTP code, email, and new password</param>
    /// <returns>Confirmation that password was updated</returns>
    /// <response code="200">Password updated successfully</response>
    /// <response code="400">Invalid or expired OTP, or validation error</response>
    [HttpPost("password/verify-otp")]
    public async Task<IActionResult> VerifyPasswordOtp([FromBody] VerifyPasswordOtpRequest request)
    {
        var minPasswordLength = _config.GetValue("AppSettings:MinPasswordLength", 6);

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(ApiResponse.Fail("A valid email is required"));
        if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Length != 6 || !request.Otp.All(char.IsDigit))
            return BadRequest(ApiResponse.Fail("OTP must be 6 digits"));
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < minPasswordLength)
            return BadRequest(ApiResponse.Fail($"Password must be at least {minPasswordLength} characters"));

        var otpRecord = await _db.OtpVerifications
            .Where(o => o.Email == request.Email && o.Purpose == OtpPurpose.PasswordReset && !o.IsVerified)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null || otpRecord.Otp != request.Otp || otpRecord.ExpiresAt < DateTime.UtcNow)
            return BadRequest(ApiResponse.Fail("Invalid or expired OTP"));

        otpRecord.IsVerified = true;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return BadRequest(ApiResponse.Fail("User not found"));

        user.PasswordHash = _auth.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Password updated successfully"));
    }

    /// <summary>
    /// Refreshes the JWT access token using a refresh token cookie.
    /// </summary>
    /// <returns>New JWT access token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Missing or invalid refresh token</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // In production, validate refresh token from cookie against stored tokens
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(ApiResponse.Fail("No refresh token"));

        // For this implementation, we extract user from the existing access token header
        var userId = _currentUser.UserId;
        if (userId == null)
            return Unauthorized(ApiResponse.Fail("Invalid session"));

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
            return Unauthorized(ApiResponse.Fail("User not found"));

        var accessToken = _auth.GenerateAccessToken(user);
        var newRefreshToken = _auth.GenerateRefreshToken();
        SetRefreshCookie(newRefreshToken);

        return Ok(ApiResponse<object>.Ok(new { accessToken }));
    }

    /// <summary>
    /// Logs out the current user by clearing the refresh token cookie.
    /// </summary>
    /// <returns>Confirmation of logout</returns>
    /// <response code="200">Logged out successfully</response>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(ApiResponse.Ok("Logged out"));
    }

    /// <summary>
    /// Changes the authenticated user's password. Verifies the current password
    /// before applying the new one.
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <response code="200">Password updated</response>
    /// <response code="400">Invalid input or wrong current password</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var minPasswordLength = _config.GetValue("AppSettings:MinPasswordLength", 6);
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < minPasswordLength)
            return BadRequest(ApiResponse.Fail($"New password must be at least {minPasswordLength} characters"));

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(ApiResponse.Fail("Account not found or password not set"));

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Current password is incorrect"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "Password updated successfully" }));
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user.
    /// </summary>
    /// <returns>User profile data</returns>
    /// <response code="200">User profile retrieved</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse.Fail("User not found"));

        return Ok(ApiResponse<object>.Ok(MapUser(user)));
    }

    private void SetRefreshCookie(string refreshToken)
    {
        var refreshTokenDays = _config.GetValue("AppSettings:RefreshTokenExpirationDays", 7);

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenDays),
            Path = "/"
        });
    }

    private static object MapUser(User user) => new
    {
        user.Id,
        user.OpenId,
        user.Name,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Phone,
        role = user.Role.ToString().ToLowerInvariant(),
        user.IsEmailVerified,
        user.CreatedAt
    };
}
