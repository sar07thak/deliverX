using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DeliverX.Application.DTOs.Registration;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;
using DeliverX.Infrastructure.Services.External;
using DeliverX.Infrastructure.Utilities;

namespace DeliverX.Infrastructure.Services;

public interface IBankVerificationService
{
    Task<VerificationResult> VerifyBankAccountAsync(
        Guid userId,
        string accountNumber,
        string ifscCode,
        string accountHolderName,
        string method = "PENNY_DROP",
        CancellationToken ct = default);

    Task<VerificationResult> GetVerificationStatusAsync(Guid kycId, CancellationToken ct = default);
}

public class BankVerificationService : IBankVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IBankVerificationClient _bankClient;
    private readonly IEncryptionHelper _encryption;
    private readonly INameMatchHelper _nameMatch;
    private readonly ILogger<BankVerificationService> _logger;

    public BankVerificationService(
        ApplicationDbContext context,
        IBankVerificationClient bankClient,
        IEncryptionHelper encryption,
        INameMatchHelper nameMatch,
        ILogger<BankVerificationService> logger)
    {
        _context = context;
        _bankClient = bankClient;
        _encryption = encryption;
        _nameMatch = nameMatch;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyBankAccountAsync(
        Guid userId,
        string accountNumber,
        string ifscCode,
        string accountHolderName,
        string method = "PENNY_DROP",
        CancellationToken ct = default)
    {
        _logger.LogInformation("Verifying bank account for user {UserId} using {Method}", userId, method);

        // Generate hash for duplicate detection (NEVER store in plain text)
        var accountHash = HashHelper.SHA256(accountNumber);

        // Call bank verification API
        var verificationResult = await _bankClient.VerifyBankAccountAsync(
            accountNumber, ifscCode, accountHolderName, method);

        if (!verificationResult.IsSuccess)
        {
            _logger.LogWarning("Bank verification failed for user {UserId}: {Error}", userId, verificationResult.ErrorMessage);
            return VerificationResult.Failure("BANK_VERIFICATION_FAILED", verificationResult.ErrorMessage ?? "Bank verification failed");
        }

        // Get Aadhaar name for cross-check
        var aadhaarVerification = await _context.Set<AadhaarVerification>()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        int nameMatchScore = 0;
        if (aadhaarVerification != null)
        {
            var aadhaarName = _encryption.Decrypt(aadhaarVerification.NameAsPerAadhaar);
            nameMatchScore = _nameMatch.CalculateSimilarity(aadhaarName, verificationResult.BankDetails!.AccountHolderName);

            if (nameMatchScore < 70)
            {
                _logger.LogWarning("Bank account holder name mismatch for user {UserId}. Aadhaar: {AadhaarName}, Bank: {BankName}, Score: {Score}",
                    userId, aadhaarName, verificationResult.BankDetails.AccountHolderName, nameMatchScore);
            }
        }

        // Store verification
        var verification = new BankVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumberEncrypted = _encryption.Encrypt(accountNumber),
            AccountNumberHash = accountHash,
            IFSCCode = ifscCode,
            AccountHolderName = _encryption.Encrypt(verificationResult.BankDetails!.AccountHolderName),
            BankName = verificationResult.BankDetails.BankName,
            BranchName = verificationResult.BankDetails.BranchName,
            VerificationMethod = method,
            TransactionId = verificationResult.TransactionId,
            NameMatchScore = nameMatchScore,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<BankVerification>().Add(verification);

        // Update or create KYC request
        var kycRequest = await _context.Set<KYCRequest>()
            .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == "BANK", ct);

        if (kycRequest == null)
        {
            kycRequest = new KYCRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VerificationType = "BANK",
                Method = method,
                InitiatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<KYCRequest>().Add(kycRequest);
        }

        kycRequest.Status = "VERIFIED";
        kycRequest.CompletedAt = DateTime.UtcNow;
        kycRequest.UpdatedAt = DateTime.UtcNow;
        kycRequest.ResponseData = JsonSerializer.Serialize(new
        {
            TransactionId = verificationResult.TransactionId,
            BankName = verificationResult.BankDetails.BankName,
            NameMatchScore = nameMatchScore
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bank verification completed for user {UserId}", userId);

        // Mask account number in response
        var maskedAccountNumber = new string('*', accountNumber.Length - 4) + accountNumber[^4..];

        return VerificationResult.Success(new
        {
            AccountNumber = maskedAccountNumber,
            IFSCCode = ifscCode,
            AccountHolderName = verificationResult.BankDetails.AccountHolderName,
            BankName = verificationResult.BankDetails.BankName,
            BranchName = verificationResult.BankDetails.BranchName,
            NameMatchScore = nameMatchScore,
            TransactionId = verificationResult.TransactionId
        }, "Bank account verified successfully");
    }

    public async Task<VerificationResult> GetVerificationStatusAsync(Guid kycId, CancellationToken ct = default)
    {
        var kycRequest = await _context.Set<KYCRequest>()
            .FirstOrDefaultAsync(k => k.Id == kycId && k.VerificationType == "BANK", ct);

        if (kycRequest == null)
        {
            return VerificationResult.Failure("KYC_NOT_FOUND", "KYC request not found");
        }

        return new VerificationResult
        {
            IsSuccess = true,
            KycId = kycId,
            Status = kycRequest.Status,
            Message = kycRequest.Status == "VERIFIED" ? "Bank account verified" : "Verification in progress",
            VerifiedData = kycRequest.ResponseData != null ? JsonSerializer.Deserialize<object>(kycRequest.ResponseData) : null
        };
    }
}
