using DeliverX.Application.Common;
using DeliverX.Application.DTOs.Auth;

namespace DeliverX.Application.Services;

public interface IAuthService
{
    /// <summary>
    /// Send OTP to user's phone
    /// </summary>
    Task<Result<OtpSendResponse>> SendOtpAsync(OtpSendRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify OTP and return authentication tokens
    /// </summary>
    Task<Result<TokenResponse>> VerifyOtpAsync(OtpVerifyRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Login with email and password
    /// </summary>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout user (revoke session)
    /// </summary>
    Task<Result> LogoutAsync(Guid userId, string refreshToken, bool logoutAll, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<Result<SessionListResponse>> GetSessionsAsync(Guid userId, string currentDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    Task<Result> RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
}
