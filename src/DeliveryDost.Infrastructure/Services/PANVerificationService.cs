using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using DeliveryDost.Application.DTOs.Registration;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;
using DeliveryDost.Infrastructure.Services.External;
using DeliveryDost.Infrastructure.Utilities;

namespace DeliveryDost.Infrastructure.Services;

public interface IPANVerificationService
{
    Task<VerificationResult> VerifyPANAsync(Guid userId, string pan, CancellationToken ct = default);
}

public class PANVerificationService : IPANVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INSDLPANClient _nsdlClient;
    private readonly IEncryptionHelper _encryption;
    private readonly INameMatchHelper _nameMatch;
    private readonly ILogger<PANVerificationService> _logger;

    public PANVerificationService(
        ApplicationDbContext context,
        INSDLPANClient nsdlClient,
        IEncryptionHelper encryption,
        INameMatchHelper nameMatch,
        ILogger<PANVerificationService> logger)
    {
        _context = context;
        _nsdlClient = nsdlClient;
        _encryption = encryption;
        _nameMatch = nameMatch;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyPANAsync(Guid userId, string pan, CancellationToken ct = default)
    {
        _logger.LogInformation("Verifying PAN for user {UserId}", userId);

        // Validate PAN format
        if (!Regex.IsMatch(pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"))
        {
            _logger.LogWarning("Invalid PAN format: {PAN}", pan);
            return VerificationResult.Failure("INVALID_PAN_FORMAT", "Invalid PAN format. Expected format: ABCDE1234F");
        }

        // Call NSDL API
        var panDetails = await _nsdlClient.VerifyPANAsync(pan);
        if (panDetails == null || panDetails.Status != "ACTIVE")
        {
            _logger.LogWarning("PAN not found or inactive: {PAN}", pan);
            return VerificationResult.Failure("INVALID_PAN", "PAN is invalid or inactive");
        }

        // Get Aadhaar name for cross-check
        var aadhaarVerification = await _context.Set<AadhaarVerification>()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        int nameMatchScore = 0;
        if (aadhaarVerification != null)
        {
            var aadhaarName = _encryption.Decrypt(aadhaarVerification.NameAsPerAadhaar);
            nameMatchScore = _nameMatch.CalculateSimilarity(aadhaarName, panDetails.Name);

            if (nameMatchScore < 70)
            {
                _logger.LogWarning("PAN name mismatch for user {UserId}. Aadhaar: {AadhaarName}, PAN: {PANName}, Score: {Score}",
                    userId, aadhaarName, panDetails.Name, nameMatchScore);
            }
        }

        // Store verification
        var verification = new PANVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PAN = pan,
            NameAsPerPAN = _encryption.Encrypt(panDetails.Name),
            DOB = panDetails.DOB,
            PANStatus = panDetails.Status,
            NameMatchScore = nameMatchScore,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<PANVerification>().Add(verification);

        // Update or create KYC request
        var kycRequest = await _context.Set<KYCRequest>()
            .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == "PAN", ct);

        if (kycRequest == null)
        {
            kycRequest = new KYCRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VerificationType = "PAN",
                Method = "API",
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
            PAN = pan,
            Name = panDetails.Name,
            Status = panDetails.Status,
            NameMatchScore = nameMatchScore
        });

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("PAN verification completed for user {UserId} with match score {Score}", userId, nameMatchScore);

        return VerificationResult.Success(new
        {
            PAN = pan,
            Name = panDetails.Name,
            DOB = panDetails.DOB,
            Status = panDetails.Status,
            NameMatchScore = nameMatchScore
        }, "PAN verified successfully");
    }
}
