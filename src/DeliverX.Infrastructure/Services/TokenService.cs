using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DeliverX.Application.Configuration;
using DeliverX.Application.DTOs.Auth;
using DeliverX.Application.Services;
using DeliverX.Domain.Entities;
using DeliverX.Infrastructure.Data;

namespace DeliverX.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ISessionService _sessionService;

    public TokenService(
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ISessionService sessionService)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _sessionService = sessionService;
    }

    public async Task<TokenResponse> GenerateTokensAsync(User user, string deviceId, CancellationToken cancellationToken = default)
    {
        // Get user permissions
        var permissions = await GetUserPermissionsAsync(user.Role, cancellationToken);

        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role),
            new("deviceId", deviceId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add phone or email
        if (!string.IsNullOrEmpty(user.Phone))
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // Generate JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate refresh token
        var refreshToken = Guid.NewGuid().ToString();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60, // Convert to seconds
            User = new UserDto
            {
                Id = user.Id,
                Role = user.Role,
                Phone = user.Phone,
                Email = user.Email,
                ProfileComplete = !string.IsNullOrEmpty(user.Phone) || !string.IsNullOrEmpty(user.Email)
            }
        };
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Get session by refresh token
        var session = await _sessionService.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken);

        if (session == null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        // Get user
        var user = await _context.Users.FindAsync(new object[] { session.UserId }, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        // Generate new tokens
        var tokens = await GenerateTokensAsync(user, session.DeviceId ?? string.Empty, cancellationToken);

        // Update session activity
        await _sessionService.UpdateSessionActivityAsync(session.Id, cancellationToken);

        return tokens;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string role, CancellationToken cancellationToken = default)
    {
        var permissions = await _context.RolePermissions
            .Where(rp => rp.Role == role)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
