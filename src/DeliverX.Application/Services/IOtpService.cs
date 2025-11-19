using DeliverX.Application.Common;

namespace DeliverX.Application.Services;

public interface IOtpService
{
    /// <summary>
    /// Generate a 6-digit OTP and store it in the database
    /// </summary>
    Task<Result<string>> GenerateAndSendOtpAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify the OTP entered by the user
    /// </summary>
    Task<Result> VerifyOtpAsync(string phone, string otp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check rate limit for OTP requests
    /// </summary>
    Task<Result> CheckRateLimitAsync(string phone, CancellationToken cancellationToken = default);
}
