using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Registration;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public interface IDuplicateDetectionService
{
    Task<DuplicateCheckResult> CheckDuplicatesAsync(
        string? phone,
        string? aadhaarHash,
        string? pan,
        string? bankAccountHash,
        CancellationToken ct = default);
}

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DuplicateDetectionService> _logger;

    public DuplicateDetectionService(
        ApplicationDbContext context,
        ILogger<DuplicateDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DuplicateCheckResult> CheckDuplicatesAsync(
        string? phone,
        string? aadhaarHash,
        string? pan,
        string? bankAccountHash,
        CancellationToken ct = default)
    {
        var result = new DuplicateCheckResult();

        // Check phone
        if (!string.IsNullOrEmpty(phone))
        {
            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == phone, ct);

            if (phoneExists)
            {
                result.IsDuplicate = true;
                result.DuplicateFields.Add("Phone");
                result.ExistingUserId = await _context.Users
                    .Where(u => u.Phone == phone)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync(ct);

                _logger.LogWarning("Duplicate phone detected: {Phone}", phone);
            }
        }

        // Check Aadhaar hash
        if (!string.IsNullOrEmpty(aadhaarHash))
        {
            var aadhaarExists = await _context.Set<Domain.Entities.AadhaarVerification>()
                .AnyAsync(a => a.AadhaarHash == aadhaarHash, ct);

            if (aadhaarExists)
            {
                result.IsDuplicate = true;
                result.DuplicateFields.Add("Aadhaar");
                _logger.LogWarning("Duplicate Aadhaar hash detected");
            }
        }

        // Check PAN (allow same PAN for different roles, but check within same role)
        if (!string.IsNullOrEmpty(pan))
        {
            var panCount = await _context.Set<Domain.Entities.PANVerification>()
                .CountAsync(p => p.PAN == pan, ct);

            if (panCount > 0)
            {
                result.Warnings.Add($"PAN already used in {panCount} account(s). Review required.");
                _logger.LogInformation("PAN {PAN} already used in {Count} accounts", pan, panCount);
            }
        }

        // Check bank account (flag if used by more than 1 DP)
        if (!string.IsNullOrEmpty(bankAccountHash))
        {
            var bankCount = await _context.Set<Domain.Entities.BankVerification>()
                .CountAsync(b => b.AccountNumberHash == bankAccountHash, ct);

            if (bankCount >= 1)
            {
                result.Warnings.Add("Bank account already used by another user. Flagged for review.");
                _logger.LogWarning("Bank account hash already used by {Count} users", bankCount);
            }
        }

        return result;
    }
}
