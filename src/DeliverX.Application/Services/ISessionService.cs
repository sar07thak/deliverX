using DeliverX.Application.Common;
using DeliverX.Application.DTOs.Auth;
using DeliverX.Domain.Entities;

namespace DeliverX.Application.Services;

public interface ISessionService
{
    /// <summary>
    /// Create a new session for a user
    /// </summary>
    Task<Result<UserSession>> CreateSessionAsync(
        Guid userId,
        string refreshToken,
        string deviceId,
        string? deviceType,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<List<SessionDto>> GetUserSessionsAsync(Guid userId, string currentDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    Task<Result> RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions for a user
    /// </summary>
    Task<Result> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate refresh token and get associated session
    /// </summary>
    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update session last active time
    /// </summary>
    Task UpdateSessionActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
