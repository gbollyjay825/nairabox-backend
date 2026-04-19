using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Interfaces;
using Nairabox.Application.Common.Models;
using Nairabox.Domain.Entities;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

[ApiController]
[Route("api/payouts")]
[Authorize]
public class PayoutsController : ControllerBase
{
    private readonly NairaboxDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PayoutsController(NairaboxDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
