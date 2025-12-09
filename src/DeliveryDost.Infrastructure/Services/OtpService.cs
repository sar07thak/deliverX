using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _otpLength;
    private readonly int _expirationMinutes;
    private readonly int _maxAttempts;
    private readonly int _rateLimitPerHour;

    public OtpService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _otpLength = configuration.GetValue<int>("OtpSettings:Length", 6);
        _expirationMinutes = configuration.GetValue<int>("OtpSettings:ExpirationMinutes", 5);
        _maxAttempts = configuration.GetValue<int>("OtpSettings:MaxAttempts", 3);
        _rateLimitPerHour = configuration.GetValue<int>("OtpSettings:RateLimitPerHour", 5);
    }

    public async Task<Result<string>> GenerateAndSendOtpAsync(string phone, CancellationToken cancellationToken = default)
    {
        // Check rate limit
        var rateLimitResult = await CheckRateLimitAsync(phone, cancellationToken);
        if (!rateLimitResult.IsSuccess)
        {
            return Result<string>.Failure(rateLimitResult.ErrorMessage!, rateLimitResult.ErrorCode);
        }

        // Generate OTP
        var otp = GenerateOtp();
        var otpHash = HashOtp(otp);

        // Store OTP in database
        var otpVerification = new OTPVerification
        {
            Phone = phone,
            OTPHash = otpHash,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            IsVerified = false
        };

        _context.OTPVerifications.Add(otpVerification);
        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Send OTP via SMS gateway (Twilio/MSG91)
        // For now, return OTP in development mode (remove in production!)
        return Result<string>.Success($"OTP: {otp} (expires in {_expirationMinutes} minutes)");
    }

    public async Task<Result> VerifyOtpAsync(string phone, string otp, CancellationToken cancellationToken = default)
    {
        // Get the latest OTP for this phone
        var otpVerification = await _context.OTPVerifications
            .Where(o => o.Phone == phone && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpVerification == null)
        {
            return Result.Failure("OTP expired or invalid", "OTP_EXPIRED");
        }

        // Check attempts
        if (otpVerification.Attempts >= _maxAttempts)
        {
            return Result.Failure("Maximum OTP attempts exceeded", "MAX_ATTEMPTS_EXCEEDED");
        }

        // Verify OTP
        if (!VerifyOtpHash(otp, otpVerification.OTPHash))
        {
            otpVerification.Attempts++;
            await _context.SaveChangesAsync(cancellationToken);

            var attemptsRemaining = _maxAttempts - otpVerification.Attempts;
            return Result.Failure($"Invalid OTP. {attemptsRemaining} attempts remaining", "INVALID_OTP");
        }

        // Mark as verified
        otpVerification.IsVerified = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CheckRateLimitAsync(string phone, CancellationToken cancellationToken = default)
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentOtpCount = await _context.OTPVerifications
            .CountAsync(o => o.Phone == phone && o.CreatedAt > oneHourAgo, cancellationToken);

        if (recentOtpCount >= _rateLimitPerHour)
        {
            return Result.Failure(
                $"Rate limit exceeded. Maximum {_rateLimitPerHour} OTP requests per hour",
                "RATE_LIMIT_EXCEEDED");
        }

        return Result.Success();
    }

    private string GenerateOtp()
    {
        // Generate cryptographically secure random OTP
        var number = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, _otpLength));
        return number.ToString($"D{_otpLength}");
    }

    private string HashOtp(string otp)
    {
        // Use SHA256 for hashing OTP
        var bytes = System.Text.Encoding.UTF8.GetBytes(otp);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyOtpHash(string otp, string otpHash)
    {
        var computedHash = HashOtp(otp);
        return computedHash == otpHash;
    }
}
