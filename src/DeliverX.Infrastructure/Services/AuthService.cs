using Microsoft.EntityFrameworkCore;
using DeliverX.Application.Common;
using DeliverX.Application.DTOs.Auth;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Domain.Enums;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        ApplicationDbContext context,
        IOtpService otpService,
        ITokenService tokenService,
        ISessionService sessionService,
        IAuditService auditService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _otpService = otpService;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<OtpSendResponse>> SendOtpAsync(OtpSendRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _otpService.GenerateAndSendOtpAsync(request.Phone, cancellationToken);

        if (result.IsSuccess)
        {
            await _auditService.LogAuthEventAsync(
                AuthEventType.OtpSent,
                phone: request.Phone,
                cancellationToken: cancellationToken);

            return Result<OtpSendResponse>.Success(new OtpSendResponse
            {
                Status = "OTP_SENT",
                ExpiresIn = 300, // 5 minutes
                Message = $"OTP sent successfully. {result.Data}" // Remove in production!
            });
        }

        return Result<OtpSendResponse>.Failure(result.ErrorMessage!, result.ErrorCode);
    }

    public async Task<Result<TokenResponse>> VerifyOtpAsync(
        OtpVerifyRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // Verify OTP
        var otpResult = await _otpService.VerifyOtpAsync(request.Phone, request.Otp, cancellationToken);
        if (!otpResult.IsSuccess)
        {
            await _auditService.LogAuthEventAsync(
                AuthEventType.OtpFailed,
                phone: request.Phone,
                ipAddress: ipAddress,
                userAgent: userAgent,
                cancellationToken: cancellationToken);

            return Result<TokenResponse>.Failure(otpResult.ErrorMessage!, otpResult.ErrorCode);
        }

        // Get or create user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone, cancellationToken);
        if (user == null)
        {
            // Parse role from request or default to EC
            var role = UserRole.EC;
            if (!string.IsNullOrEmpty(request.Role))
            {
                role = request.Role.ToUpper() switch
                {
                    "EC" => UserRole.EC,
                    "BC" => UserRole.DBC,      // Business Consumer = DBC
                    "DBC" => UserRole.DBC,
                    "DP" => UserRole.DP,
                    "DPCM" => UserRole.DPCM,
                    "SA" => UserRole.SuperAdmin,
                    "SUPERADMIN" => UserRole.SuperAdmin,
                    _ => UserRole.EC
                };
            }

            user = new User
            {
                Phone = request.Phone,
                Role = role,
                IsPhoneVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var tokens = await _tokenService.GenerateTokensAsync(user, request.DeviceId ?? string.Empty, cancellationToken);

        // Create session
        await _sessionService.CreateSessionAsync(
            user.Id,
            tokens.RefreshToken,
            request.DeviceId ?? string.Empty,
            null, // Device type
            ipAddress,
            userAgent,
            cancellationToken);

        // Log success
        await _auditService.LogAuthEventAsync(
            AuthEventType.LoginSuccess,
            userId: user.Id,
            phone: request.Phone,
            ipAddress: ipAddress,
            userAgent: userAgent,
            cancellationToken: cancellationToken);

        return Result<TokenResponse>.Success(tokens);
    }

    public async Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            await _auditService.LogAuthEventAsync(
                AuthEventType.LoginFailed,
                email: request.Email,
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: new { Reason = "User not found" },
                cancellationToken: cancellationToken);

            return Result<TokenResponse>.Failure("Invalid email or password", "INVALID_CREDENTIALS");
        }

        // Check if account is locked
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            var remainingMinutes = (user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            return Result<TokenResponse>.Failure(
                $"Account locked. Try again in {Math.Ceiling(remainingMinutes)} minutes",
                "ACCOUNT_LOCKED");
        }

        // Verify password using BCrypt
        var isPasswordValid = !string.IsNullOrEmpty(user.PasswordHash) &&
                             _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30); // Lock for 30 minutes
                await _auditService.LogAuthEventAsync(
                    AuthEventType.AccountLocked,
                    userId: user.Id,
                    email: request.Email,
                    ipAddress: ipAddress,
                    cancellationToken: cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAuthEventAsync(
                AuthEventType.LoginFailed,
                userId: user.Id,
                email: request.Email,
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: new { Reason = "Invalid password", Attempts = user.FailedLoginAttempts },
                cancellationToken: cancellationToken);

            return Result<TokenResponse>.Failure("Invalid email or password", "INVALID_CREDENTIALS");
        }

        // Check 2FA if enabled
        if (user.Is2FAEnabled)
        {
            if (string.IsNullOrEmpty(request.TotpCode))
            {
                return Result<TokenResponse>.Failure("2FA code required", "2FA_REQUIRED");
            }

            // TODO: Verify TOTP code
            var isTotpValid = VerifyTotp(request.TotpCode, user.TotpSecret ?? string.Empty);
            if (!isTotpValid)
            {
                await _auditService.LogAuthEventAsync(
                    AuthEventType.LoginFailed,
                    userId: user.Id,
                    email: request.Email,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: new { Reason = "Invalid 2FA code" },
                    cancellationToken: cancellationToken);

                return Result<TokenResponse>.Failure("Invalid 2FA code", "INVALID_2FA");
            }
        }

        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var tokens = await _tokenService.GenerateTokensAsync(user, request.DeviceId ?? string.Empty, cancellationToken);

        // Create session
        await _sessionService.CreateSessionAsync(
            user.Id,
            tokens.RefreshToken,
            request.DeviceId ?? string.Empty,
            null,
            ipAddress,
            userAgent,
            cancellationToken);

        // Log success
        await _auditService.LogAuthEventAsync(
            AuthEventType.LoginSuccess,
            userId: user.Id,
            email: request.Email,
            ipAddress: ipAddress,
            userAgent: userAgent,
            cancellationToken: cancellationToken);

        return Result<TokenResponse>.Success(tokens);
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokens = await _tokenService.RefreshTokenAsync(refreshToken, cancellationToken);

        if (tokens == null)
        {
            return Result<TokenResponse>.Failure("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");
        }

        await _auditService.LogAuthEventAsync(
            AuthEventType.TokenRefreshed,
            userId: tokens.User?.Id,
            cancellationToken: cancellationToken);

        return Result<TokenResponse>.Success(tokens);
    }

    public async Task<Result> LogoutAsync(Guid userId, string refreshToken, bool logoutAll, CancellationToken cancellationToken = default)
    {
        if (logoutAll)
        {
            await _sessionService.RevokeAllSessionsAsync(userId, cancellationToken);
        }
        else
        {
            var session = await _sessionService.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken);
            if (session != null)
            {
                await _sessionService.RevokeSessionAsync(session.Id, userId, cancellationToken);
            }
        }

        await _auditService.LogAuthEventAsync(
            AuthEventType.SessionRevoked,
            userId: userId,
            details: new { LogoutAll = logoutAll },
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    public async Task<Result<SessionListResponse>> GetSessionsAsync(Guid userId, string currentDeviceId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionService.GetUserSessionsAsync(userId, currentDeviceId, cancellationToken);

        return Result<SessionListResponse>.Success(new SessionListResponse
        {
            Sessions = sessions
        });
    }

    public async Task<Result> RevokeSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.RevokeSessionAsync(sessionId, userId, cancellationToken);

        if (result.IsSuccess)
        {
            await _auditService.LogAuthEventAsync(
                AuthEventType.SessionRevoked,
                userId: userId,
                details: new { SessionId = sessionId },
                cancellationToken: cancellationToken);
        }

        return result;
    }

    // TOTP verification - implemented below
    private bool VerifyTotp(string totpCode, string totpSecret)
    {
        if (string.IsNullOrEmpty(totpSecret) || string.IsNullOrEmpty(totpCode))
            return false;

        try
        {
            var totp = new OtpNet.Totp(System.Convert.FromBase64String(totpSecret));
            return totp.VerifyTotp(totpCode, out _, new OtpNet.VerificationWindow(2, 2));
        }
        catch
        {
            return false;
        }
    }
}
