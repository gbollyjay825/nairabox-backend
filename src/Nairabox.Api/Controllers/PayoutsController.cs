using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Domain.Enums;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

[ApiController]
[Route("api/payouts")]
[Authorize]
public class PayoutsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public PayoutsController(NairaboxDbContext db, ICurrentUserService currentUser, IConfiguration config)
    {
        _db = db;
        _currentUser = currentUser;
        _config = config;
    }

    public record SetBankAccountRequest(string BankCode, string BankName, string AccountNumber, string AccountName);

    [HttpPost("bank-account")]
    public async Task<IActionResult> SetBankAccount([FromBody] SetBankAccountRequest request)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var existing = await _db.BankAccounts.FirstOrDefaultAsync(b => b.OrganizerId == userId.Value);

        if (existing != null)
        {
            existing.BankCode = request.BankCode;
            existing.BankName = request.BankName;
            existing.AccountNumber = request.AccountNumber;
            existing.AccountName = request.AccountName;
            existing.IsVerified = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var bankAccount = new BankAccount
            {
                OrganizerId = userId.Value,
                BankCode = request.BankCode,
                BankName = request.BankName,
                AccountNumber = request.AccountNumber,
                AccountName = request.AccountName
            };
            _db.BankAccounts.Add(bankAccount);
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Bank account saved"));
    }

    [HttpGet("bank-account")]
    public async Task<IActionResult> GetBankAccount()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var account = await _db.BankAccounts.FirstOrDefaultAsync(b => b.OrganizerId == userId.Value);
        if (account == null) return NotFound(ApiResponse.Fail("No bank account found"));

        return Ok(ApiResponse<object>.Ok(new
        {
            account.Id,
            account.BankCode,
            account.BankName,
            account.AccountNumber,
            account.AccountName,
            account.IsVerified,
            account.CreatedAt
        }));
    }

    /// <summary>Verify bank account</summary>
    [HttpPost("bank-account/verify")]
    public async Task<IActionResult> VerifyBankAccount()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var account = await _db.BankAccounts.FirstOrDefaultAsync(a => a.OrganizerId == userId.Value);
        if (account == null) return NotFound(ApiResponse.Fail("No bank account found"));

        account.IsVerified = true;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<BankAccount>.Ok(account));
    }

    /// <summary>Get payout for a specific event</summary>
    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetEventPayout(int eventId)
    {
        var payout = await _db.Payouts.FirstOrDefaultAsync(p => p.EventId == eventId);
        if (payout == null) return NotFound(ApiResponse.Fail("No payout found for this event"));
        return Ok(ApiResponse<Payout>.Ok(payout));
    }

    /// <summary>Request a payout for an event</summary>
    [HttpPost("request/{eventId:int}")]
    public async Task<IActionResult> RequestPayout(int eventId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId.Value);
        if (ev == null) return NotFound(ApiResponse.Fail("Event not found"));

        var existing = await _db.Payouts.FirstOrDefaultAsync(p => p.EventId == eventId);
        if (existing != null) return BadRequest(ApiResponse.Fail("Payout already requested"));

        var feeRate = decimal.Parse(_config["AppSettings:ServiceFeePercentage"] ?? "0.035");
        var totalRevenue = ev.TotalRevenue;
        var serviceFee = totalRevenue * feeRate;
        var transferFee = 100m;

        var payout = new Payout
        {
            EventId = eventId,
            OrganizerId = userId.Value,
            TotalRevenue = totalRevenue,
            ServiceFee = serviceFee,
            TransferFee = transferFee,
            PayoutAmount = totalRevenue - serviceFee - transferFee,
            Status = PayoutStatus.Pending,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Payouts.Add(payout);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEventPayout), new { eventId }, ApiResponse<Payout>.Ok(payout));
    }

    /// <summary>Cancel a pending payout</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelPayout(int id)
    {
        var payout = await _db.Payouts.FindAsync(id);
        if (payout == null) return NotFound(ApiResponse.Fail("Payout not found"));
        if (payout.Status != PayoutStatus.Pending) return BadRequest(ApiResponse.Fail("Only pending payouts can be cancelled"));

        _db.Payouts.Remove(payout);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Payout cancelled"));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPayoutHistory()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized(ApiResponse.Fail("Not authenticated"));

        var payouts = await _db.Payouts
            .Include(p => p.Event)
            .Where(p => p.OrganizerId == userId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(payouts.Select(p => new
        {
            p.Id,
            p.EventId,
            eventName = p.Event.Name,
            p.TotalRevenue,
            p.ServiceFee,
            p.PayoutAmount,
            p.TransferFee,
            status = p.Status.ToString().ToLowerInvariant(),
            p.PaymentReference,
            p.TransactionId,
            p.ScheduledDate,
            p.PaidDate,
            p.FailureReason,
            p.CreatedAt
        })));
    }
}
