using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DeliverX.Application.DTOs.Registration;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public interface IDPRegistrationService
{
    Task<DPRegistrationResponse> InitiateRegistrationAsync(string phone, string? referralCode, CancellationToken ct = default);
    Task<ProfileCompleteResponse> CompleteProfileAsync(Guid userId, ProfileCompleteRequest request, CancellationToken ct = default);
    Task<KYCStatusResponse> GetKYCStatusAsync(Guid userId, CancellationToken ct = default);
}

public class DPRegistrationService : IDPRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly ILogger<DPRegistrationService> _logger;

    public DPRegistrationService(
        ApplicationDbContext context,
        IDuplicateDetectionService duplicateDetection,
        ILogger<DPRegistrationService> logger)
    {
        _context = context;
        _duplicateDetection = duplicateDetection;
        _logger = logger;
    }

    public async Task<DPRegistrationResponse> InitiateRegistrationAsync(
        string phone,
        string? referralCode,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating DP registration for phone {Phone}", phone);

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (existingUser != null)
        {
            // User exists - check if already a DP
            var existingProfile = await _context.Set<DeliveryPartnerProfile>()
                .FirstOrDefaultAsync(dp => dp.UserId == existingUser.Id, ct);

            if (existingProfile != null)
            {
                _logger.LogWarning("User {UserId} already has a DP profile", existingUser.Id);
                return new DPRegistrationResponse
                {
                    UserId = existingUser.Id,
                    RegistrationId = existingProfile.Id,
                    Status = "ALREADY_REGISTERED",
                    Message = "This phone number is already registered as a Delivery Partner",
                    NextStep = existingProfile.IsActive ? "ACTIVE" : "COMPLETE_KYC"
                };
            }

            // User exists but not a DP - upgrade to DP role
            _logger.LogInformation("Upgrading existing user {UserId} to DP role", existingUser.Id);
            existingUser.Role = "DP";
            existingUser.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return new DPRegistrationResponse
            {
                UserId = existingUser.Id,
                Status = "PROFILE_INCOMPLETE",
                Message = "DP registration initiated. Please complete your profile.",
                NextStep = "COMPLETE_PROFILE"
            };
        }

        // New user - create account with DP role
        var user = new User
        {
            Id = Guid.NewGuid(),
            Phone = phone,
            Role = "DP",
            IsPhoneVerified = false, // Will be verified via OTP
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User created with ID {UserId} for phone {Phone}", user.Id, phone);

        return new DPRegistrationResponse
        {
            UserId = user.Id,
            Status = "VERIFY_PHONE",
            Message = "Please verify your phone number with OTP",
            NextStep = "VERIFY_OTP"
        };
    }

    public async Task<ProfileCompleteResponse> CompleteProfileAsync(
        Guid userId,
        ProfileCompleteRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Completing profile for user {UserId}", userId);

        // Verify user exists
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Update user email if provided
        if (!string.IsNullOrEmpty(request.Email))
        {
            user.Email = request.Email;
            user.UpdatedAt = DateTime.UtcNow;
        }

        // Create or update DP profile
        var existingProfile = await _context.Set<DeliveryPartnerProfile>()
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (existingProfile != null)
        {
            // Update existing profile
            UpdateProfileFields(existingProfile, request);
            existingProfile.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new profile
            var profile = new DeliveryPartnerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FullName = request.FullName,
                DOB = request.DOB,
                Gender = request.Gender,
                ProfilePhotoUrl = request.ProfilePhotoUrl,
                Address = request.Address != null ? JsonSerializer.Serialize(request.Address) : null,
                VehicleType = request.VehicleType,
                Languages = request.Languages != null ? JsonSerializer.Serialize(request.Languages) : null,
                Availability = request.Availability,
                ServiceAreaCenterLat = request.ServiceArea?.CenterLat,
                ServiceAreaCenterLng = request.ServiceArea?.CenterLng,
                ServiceAreaRadiusKm = request.ServiceArea?.RadiusKm,
                PerKmRate = request.Pricing?.PerKmRate,
                PerKgRate = request.Pricing?.PerKgRate,
                MinCharge = request.Pricing?.MinCharge,
                MaxDistanceKm = request.Pricing?.MaxDistanceKm,
                IsActive = false, // Will be activated after KYC
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<DeliveryPartnerProfile>().Add(profile);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Profile completed for user {UserId}", userId);

        return new ProfileCompleteResponse
        {
            UserId = userId,
            Status = "PROFILE_COMPLETED",
            NextStep = "KYC_VERIFICATION"
        };
    }

    public async Task<KYCStatusResponse> GetKYCStatusAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting KYC status for user {UserId}", userId);

        var kycRequests = await _context.Set<KYCRequest>()
            .Where(k => k.UserId == userId)
            .ToListAsync(ct);

        var verifications = new Dictionary<string, VerificationStatusDto>();

        // Check each verification type
        foreach (var request in kycRequests)
        {
            verifications[request.VerificationType.ToLower()] = new VerificationStatusDto
            {
                Status = request.Status,
                VerifiedAt = request.CompletedAt,
                InitiatedAt = request.InitiatedAt,
                ReferenceId = ExtractReferenceId(request.ResponseData)
            };
        }

        // Determine overall status
        var mandatoryTypes = new[] { "aadhaar", "pan", "bank" };
        var allMandatoryVerified = mandatoryTypes.All(type =>
            verifications.ContainsKey(type) && verifications[type].Status == "VERIFIED");

        var anyVerified = verifications.Any(v => v.Value.Status == "VERIFIED");
        var anyRejected = verifications.Any(v => v.Value.Status == "REJECTED");

        string overallStatus;
        if (allMandatoryVerified)
            overallStatus = "FULLY_VERIFIED";
        else if (anyRejected)
            overallStatus = "REJECTED";
        else if (anyVerified)
            overallStatus = "PARTIALLY_VERIFIED";
        else
            overallStatus = "PENDING";

        var pendingVerifications = mandatoryTypes
            .Where(type => !verifications.ContainsKey(type) || verifications[type].Status != "VERIFIED")
            .ToList();

        var canActivate = allMandatoryVerified;

        return new KYCStatusResponse
        {
            UserId = userId,
            OverallStatus = overallStatus,
            Verifications = verifications,
            CanActivate = canActivate,
            PendingVerifications = pendingVerifications,
            NextStep = canActivate ? "Start accepting deliveries" : $"Complete pending verifications: {string.Join(", ", pendingVerifications)}"
        };
    }

    private void UpdateProfileFields(DeliveryPartnerProfile profile, ProfileCompleteRequest request)
    {
        profile.FullName = request.FullName;
        profile.DOB = request.DOB;
        profile.Gender = request.Gender;
        profile.ProfilePhotoUrl = request.ProfilePhotoUrl;
        profile.Address = request.Address != null ? JsonSerializer.Serialize(request.Address) : null;
        profile.VehicleType = request.VehicleType;
        profile.Languages = request.Languages != null ? JsonSerializer.Serialize(request.Languages) : null;
        profile.Availability = request.Availability;
        profile.ServiceAreaCenterLat = request.ServiceArea?.CenterLat;
        profile.ServiceAreaCenterLng = request.ServiceArea?.CenterLng;
        profile.ServiceAreaRadiusKm = request.ServiceArea?.RadiusKm;
        profile.PerKmRate = request.Pricing?.PerKmRate;
        profile.PerKgRate = request.Pricing?.PerKgRate;
        profile.MinCharge = request.Pricing?.MinCharge;
        profile.MaxDistanceKm = request.Pricing?.MaxDistanceKm;
    }

    private string? ExtractReferenceId(string? responseData)
    {
        if (string.IsNullOrEmpty(responseData))
            return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseData);
            if (json.TryGetProperty("ReferenceId", out var refId))
                return refId.GetString();
        }
        catch
        {
            // Ignore JSON parsing errors
        }

        return null;
    }
}
