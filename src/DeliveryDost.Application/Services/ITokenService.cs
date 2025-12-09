using System.Security.Claims;
using DeliveryDost.Application.DTOs.Auth;
using DeliveryDost.Domain.Entities;

namespace DeliveryDost.Application.Services;

public interface ITokenService
{
    /// <summary>
    /// Generate JWT access token and refresh token for a user
    /// </summary>
    Task<TokenResponse> GenerateTokensAsync(User user, string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate and refresh an access token using refresh token
    /// </summary>
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JWT token and return claims
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Get user permissions for JWT claims
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(string role, CancellationToken cancellationToken = default);
}
