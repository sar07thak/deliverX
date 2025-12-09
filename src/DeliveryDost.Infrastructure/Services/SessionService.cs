using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.Configuration;
using DeliveryDost.Application.DTOs.Auth;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public SessionService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<UserSession>> CreateSessionAsync(
        Guid userId,
        string refreshToken,
        string deviceId,
        string? deviceType,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);

        var session = new UserSession
        {
            UserId = userId,
            RefreshTokenHash = refreshTokenHash,
            DeviceId = deviceId,
            DeviceType = deviceType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserSession>.Success(session);
    }

    public async Task<List<SessionDto>> GetUserSessionsAsync(Guid userId, string currentDeviceId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                DeviceType = s.DeviceType,
                DeviceId = s.DeviceId,
                IpAddress = s.IpAddress,
                Location = s.Location,
                LastActive = s.LastActiveAt,
                CreatedAt = s.CreatedAt,
                IsCurrent = s.DeviceId == currentDeviceId
            })
            .ToListAsync(cancellationToken);

        return sessions;
    }

    public async Task<Result> RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session == null)
        {
            return Result.Failure("Session not found", "SESSION_NOT_FOUND");
        }

        session.IsRevoked = true;
        session.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);

        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(
                s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        return session;
    }

    public async Task UpdateSessionActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.UserSessions.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null)
        {
            session.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
