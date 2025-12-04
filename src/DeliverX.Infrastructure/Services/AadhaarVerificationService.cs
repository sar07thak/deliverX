using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DeliverX.Application.DTOs.Registration;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;
using DeliverX.Infrastructure.Services.External;
using DeliverX.Infrastructure.Utilities;

namespace DeliverX.Infrastructure.Services;

public interface IAadhaarVerificationService
{
    Task<VerificationResult> InitiateDigiLockerVerificationAsync(Guid userId, string redirectUrl, CancellationToken ct = default);
    Task<VerificationResult> CompleteDigiLockerVerificationAsync(Guid userId, string digilockerCode, CancellationToken ct = default);
    Task<VerificationResult> ManualVerificationAsync(Guid userId, string aadhaarLast4, string documentUrl, CancellationToken ct = default);
}

public class AadhaarVerificationService : IAadhaarVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IDigiLockerClient _digiLockerClient;
    private readonly IEncryptionHelper _encryption;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly ILogger<AadhaarVerificationService> _logger;

    public AadhaarVerificationService(
        ApplicationDbContext context,
        IDigiLockerClient digiLockerClient,
        IEncryptionHelper encryption,
        IDuplicateDetectionService duplicateDetection,
        ILogger<AadhaarVerificationService> logger)
    {
        _context = context;
        _digiLockerClient = digiLockerClient;
        _encryption = encryption;
        _duplicateDetection = duplicateDetection;
        _logger = logger;
    }

    public async Task<VerificationResult> InitiateDigiLockerVerificationAsync(
        Guid userId,
        string redirectUrl,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating DigiLocker verification for user {UserId}", userId);

        // Generate DigiLocker auth URL
        var authUrl = await _digiLockerClient.GetAuthorizationUrlAsync(userId.ToString(), redirectUrl);

        // Create KYC request
        var kycRequest = new KYCRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VerificationType = "AADHAAR",
            Method = "DIGILOCKER",
            Status = "IN_PROGRESS",
            InitiatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<KYCRequest>().Add(kycRequest);
        await _context.SaveChangesAsync(ct);

        return new VerificationResult
        {
            IsSuccess = true,
            KycId = kycRequest.Id,
            Status = "IN_PROGRESS",
            RedirectUrl = authUrl,
            Message = "Redirect user to DigiLocker for authorization"
        };
    }

    public async Task<VerificationResult> CompleteDigiLockerVerificationAsync(
        Guid userId,
        string digilockerCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Completing DigiLocker verification for user {UserId} with code {Code}", userId, digilockerCode);

            // Exchange code for Aadhaar data
            _logger.LogInformation("Fetching Aadhaar data from DigiLocker client...");
            var aadhaarData = await _digiLockerClient.GetAadhaarDataAsync(digilockerCode);
            _logger.LogInformation("Aadhaar data received: Name={Name}, DOB={DOB}, Gender={Gender}",
                aadhaarData.Name, aadhaarData.DOB, aadhaarData.Gender);

            // Generate Aadhaar hash (NEVER store full number)
            var aadhaarHash = HashHelper.SHA256(aadhaarData.AadhaarNumber);
            _logger.LogInformation("Aadhaar hash generated");

            // Check duplicates
            _logger.LogInformation("Checking for duplicate Aadhaar...");
            var duplicateCheck = await _duplicateDetection.CheckDuplicatesAsync(
                phone: null,
                aadhaarHash: aadhaarHash,
                pan: null,
                bankAccountHash: null,
                ct);

            if (duplicateCheck.IsDuplicate && duplicateCheck.DuplicateFields.Contains("Aadhaar"))
            {
                _logger.LogWarning("Duplicate Aadhaar detected for user {UserId}", userId);
                return VerificationResult.Failure("AADHAAR_ALREADY_USED", "This Aadhaar is already registered in the system");
            }

            _logger.LogInformation("No duplicate found. Storing verification data...");

            // Store verification data (encrypted)
            var verification = new AadhaarVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AadhaarHash = aadhaarHash,
                AadhaarReferenceId = aadhaarData.ReferenceId,
                NameAsPerAadhaar = _encryption.Encrypt(aadhaarData.Name),
                DOB = aadhaarData.DOB,
                Gender = aadhaarData.Gender,
                AddressEncrypted = _encryption.Encrypt(JsonSerializer.Serialize(aadhaarData.Address)),
                VerificationMethod = "DIGILOCKER",
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<AadhaarVerification>().Add(verification);

            // Update KYC request status
            _logger.LogInformation("Updating KYC request status...");
            var kycRequest = await _context.Set<KYCRequest>()
                .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == "AADHAAR", ct);

            if (kycRequest != null)
            {
                kycRequest.Status = "VERIFIED";
                kycRequest.CompletedAt = DateTime.UtcNow;
                kycRequest.UpdatedAt = DateTime.UtcNow;
                kycRequest.ResponseData = JsonSerializer.Serialize(new
                {
                    ReferenceId = aadhaarData.ReferenceId,
                    VerifiedAt = DateTime.UtcNow
                });
                _logger.LogInformation("KYC request updated to VERIFIED");
            }
            else
            {
                _logger.LogWarning("No existing KYC request found for user {UserId}, creating new one", userId);
                kycRequest = new KYCRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    VerificationType = "AADHAAR",
                    Method = "DIGILOCKER",
                    Status = "VERIFIED",
                    InitiatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ResponseData = JsonSerializer.Serialize(new
                    {
                        ReferenceId = aadhaarData.ReferenceId,
                        VerifiedAt = DateTime.UtcNow
                    })
                };
                _context.Set<KYCRequest>().Add(kycRequest);
            }

            _logger.LogInformation("Saving changes to database...");
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Changes saved successfully");

            // Check if all KYC complete and activate DP
            await CheckAndActivateDPAsync(userId, ct);

            _logger.LogInformation("Aadhaar verification completed successfully for user {UserId}", userId);

            return VerificationResult.Success(new
            {
                Name = aadhaarData.Name,
                DOB = aadhaarData.DOB,
                Gender = aadhaarData.Gender,
                ReferenceId = aadhaarData.ReferenceId
            }, "Aadhaar verified successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing DigiLocker verification for user {UserId}: {Message}", userId, ex.Message);
            throw; // Re-throw to let controller handle it
        }
    }

    public async Task<VerificationResult> ManualVerificationAsync(
        Guid userId,
        string aadhaarLast4,
        string documentUrl,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating manual Aadhaar verification for user {UserId}", userId);

        // Create KYC request for manual review
        var kycRequest = new KYCRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VerificationType = "AADHAAR",
            Method = "MANUAL_UPLOAD",
            Status = "PENDING",
            InitiatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DocumentUrls = JsonSerializer.Serialize(new[] { documentUrl }),
            RequestData = JsonSerializer.Serialize(new { AadhaarLast4 = aadhaarLast4 })
        };

        _context.Set<KYCRequest>().Add(kycRequest);
        await _context.SaveChangesAsync(ct);

        return VerificationResult.Pending(kycRequest.Id, "Document uploaded. Manual verification in progress.");
    }

    private async Task CheckAndActivateDPAsync(Guid userId, CancellationToken ct)
    {
        var allVerifications = await _context.Set<KYCRequest>()
            .Where(k => k.UserId == userId)
            .ToListAsync(ct);

        // Mandatory: Aadhaar, PAN, Bank
        var mandatoryTypes = new[] { "AADHAAR", "PAN", "BANK" };
        var mandatoryVerified = mandatoryTypes.All(type =>
            allVerifications.Any(v => v.VerificationType == type && v.Status == "VERIFIED"));

        if (mandatoryVerified)
        {
            var dpProfile = await _context.Set<DeliveryPartnerProfile>()
                .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

            if (dpProfile != null && !dpProfile.IsActive)
            {
                dpProfile.IsActive = true;
                dpProfile.ActivatedAt = DateTime.UtcNow;
                dpProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("DP profile activated for user {UserId}", userId);
            }
        }
    }
}
