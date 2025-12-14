using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.BCApi;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class BCApiService : IBCApiService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BCApiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private const int AccessTokenExpiryMinutes = 60;
    private const int RefreshTokenExpiryDays = 30;

    public BCApiService(
        ApplicationDbContext context,
        ILogger<BCApiService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ========== API KEY MANAGEMENT ==========

    public async Task<ApiKeyResponse> CreateApiKeyAsync(Guid businessConsumerId, CreateApiKeyRequest request, CancellationToken ct = default)
    {
        // Generate API key and secret
        var apiKeyId = $"DDK_{GenerateRandomString(24)}";
        var apiKeySecret = GenerateRandomString(48);
        var apiKeyHash = HashString(apiKeySecret);

        var scopes = request.Scopes.Any() ? string.Join(",", request.Scopes) : "delivery:read,delivery:create";

        var credential = new BCApiCredential
        {
            Id = Guid.NewGuid(),
            BusinessConsumerId = businessConsumerId,
            ApiKeyId = apiKeyId,
            ApiKeyHash = apiKeyHash,
            Name = request.Name,
            Environment = request.Environment,
            IsActive = true,
            ExpiresAt = request.ExpiresAt,
            RateLimitPerMinute = request.RateLimitPerMinute ?? 60,
            RateLimitPerDay = request.RateLimitPerDay ?? 10000,
            AllowedIps = request.AllowedIps,
            Scopes = scopes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BCApiCredentials.Add(credential);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created API key {ApiKeyId} for BC {BusinessConsumerId}", apiKeyId, businessConsumerId);

        return new ApiKeyResponse
        {
            Id = credential.Id,
            ApiKeyId = apiKeyId,
            ApiKeySecret = apiKeySecret, // Only returned on creation
            Name = credential.Name,
            Environment = credential.Environment,
            IsActive = credential.IsActive,
            ExpiresAt = credential.ExpiresAt,
            RateLimitPerMinute = credential.RateLimitPerMinute,
            RateLimitPerDay = credential.RateLimitPerDay,
            AllowedIps = credential.AllowedIps,
            Scopes = credential.Scopes.Split(',').ToList(),
            CreatedAt = credential.CreatedAt
        };
    }

    public async Task<List<ApiKeyListDto>> GetApiKeysAsync(Guid businessConsumerId, CancellationToken ct = default)
    {
        var credentials = await _context.BCApiCredentials
            .Where(c => c.BusinessConsumerId == businessConsumerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return credentials.Select(c => new ApiKeyListDto
        {
            Id = c.Id,
            ApiKeyId = c.ApiKeyId,
            Name = c.Name,
            Environment = c.Environment,
            IsActive = c.IsActive,
            ExpiresAt = c.ExpiresAt,
            LastUsedAt = c.LastUsedAt,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<ApiKeyResponse?> GetApiKeyAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiKeyId }, ct);
        if (credential == null) return null;

        return new ApiKeyResponse
        {
            Id = credential.Id,
            ApiKeyId = credential.ApiKeyId,
            Name = credential.Name,
            Environment = credential.Environment,
            IsActive = credential.IsActive,
            ExpiresAt = credential.ExpiresAt,
            LastUsedAt = credential.LastUsedAt,
            RateLimitPerMinute = credential.RateLimitPerMinute,
            RateLimitPerDay = credential.RateLimitPerDay,
            AllowedIps = credential.AllowedIps,
            Scopes = credential.Scopes.Split(',').ToList(),
            CreatedAt = credential.CreatedAt
        };
    }

    public async Task<bool> UpdateApiKeyAsync(Guid apiKeyId, UpdateApiKeyRequest request, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiKeyId }, ct);
        if (credential == null) return false;

        if (!string.IsNullOrEmpty(request.Name))
            credential.Name = request.Name;
        if (request.RateLimitPerMinute.HasValue)
            credential.RateLimitPerMinute = request.RateLimitPerMinute.Value;
        if (request.RateLimitPerDay.HasValue)
            credential.RateLimitPerDay = request.RateLimitPerDay.Value;
        if (request.AllowedIps != null)
            credential.AllowedIps = request.AllowedIps;
        if (request.Scopes != null && request.Scopes.Any())
            credential.Scopes = string.Join(",", request.Scopes);

        credential.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated API key {ApiKeyId}", apiKeyId);
        return true;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId, RevokeApiKeyRequest request, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiKeyId }, ct);
        if (credential == null) return false;

        credential.IsActive = false;
        credential.UpdatedAt = DateTime.UtcNow;

        // Also revoke all associated tokens
        var tokens = await _context.BCOAuthTokens
            .Where(t => t.ApiCredentialId == apiKeyId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = $"API key revoked: {request.Reason}";
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Revoked API key {ApiKeyId}: {Reason}", apiKeyId, request.Reason);
        return true;
    }

    public async Task<bool> RegenerateApiKeySecretAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiKeyId }, ct);
        if (credential == null) return false;

        var newSecret = GenerateRandomString(48);
        credential.ApiKeyHash = HashString(newSecret);
        credential.UpdatedAt = DateTime.UtcNow;

        // Revoke all existing tokens
        var tokens = await _context.BCOAuthTokens
            .Where(t => t.ApiCredentialId == apiKeyId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "API key secret regenerated";
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Regenerated secret for API key {ApiKeyId}", apiKeyId);
        return true;
    }

    // ========== OAUTH TOKEN MANAGEMENT ==========

    public async Task<TokenResponse> GenerateTokenAsync(TokenRequest request, CancellationToken ct = default)
    {
        if (request.GrantType == "client_credentials")
        {
            return await GenerateTokenFromCredentialsAsync(request, ct);
        }
        else if (request.GrantType == "refresh_token")
        {
            return await RefreshTokenAsync(request, ct);
        }

        return new TokenResponse
        {
            IsSuccess = false,
            Error = "unsupported_grant_type",
            ErrorDescription = "Only client_credentials and refresh_token grant types are supported"
        };
    }

    private async Task<TokenResponse> GenerateTokenFromCredentialsAsync(TokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.ApiKeyId) || string.IsNullOrEmpty(request.ApiKeySecret))
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_request",
                ErrorDescription = "API key ID and secret are required"
            };
        }

        var credential = await _context.BCApiCredentials
            .FirstOrDefaultAsync(c => c.ApiKeyId == request.ApiKeyId && c.IsActive, ct);

        if (credential == null)
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_client",
                ErrorDescription = "Invalid API key"
            };
        }

        // Verify secret
        if (credential.ApiKeyHash != HashString(request.ApiKeySecret))
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_client",
                ErrorDescription = "Invalid API secret"
            };
        }

        // Check expiry
        if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_client",
                ErrorDescription = "API key has expired"
            };
        }

        // Generate tokens
        var accessToken = GenerateRandomString(64);
        var refreshToken = GenerateRandomString(64);
        var scopes = request.Scope ?? credential.Scopes;

        var oauthToken = new BCOAuthToken
        {
            Id = Guid.NewGuid(),
            ApiCredentialId = credential.Id,
            AccessTokenHash = HashString(accessToken),
            RefreshTokenHash = HashString(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            Scopes = scopes,
            CreatedAt = DateTime.UtcNow
        };

        _context.BCOAuthTokens.Add(oauthToken);

        // Update last used
        credential.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Generated OAuth token for API key {ApiKeyId}", request.ApiKeyId);

        return new TokenResponse
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = AccessTokenExpiryMinutes * 60,
            Scope = scopes
        };
    }

    private async Task<TokenResponse> RefreshTokenAsync(TokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_request",
                ErrorDescription = "Refresh token is required"
            };
        }

        var refreshTokenHash = HashString(request.RefreshToken);
        var existingToken = await _context.BCOAuthTokens
            .Include(t => t.ApiCredential)
            .FirstOrDefaultAsync(t => t.RefreshTokenHash == refreshTokenHash && !t.IsRevoked, ct);

        if (existingToken == null || existingToken.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return new TokenResponse
            {
                IsSuccess = false,
                Error = "invalid_grant",
                ErrorDescription = "Invalid or expired refresh token"
            };
        }

        // Revoke old token
        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedReason = "Token refreshed";

        // Generate new tokens
        var accessToken = GenerateRandomString(64);
        var refreshToken = GenerateRandomString(64);

        var newToken = new BCOAuthToken
        {
            Id = Guid.NewGuid(),
            ApiCredentialId = existingToken.ApiCredentialId,
            AccessTokenHash = HashString(accessToken),
            RefreshTokenHash = HashString(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            Scopes = existingToken.Scopes,
            CreatedAt = DateTime.UtcNow
        };

        _context.BCOAuthTokens.Add(newToken);
        await _context.SaveChangesAsync(ct);

        return new TokenResponse
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = AccessTokenExpiryMinutes * 60,
            Scope = existingToken.Scopes
        };
    }

    public async Task<bool> RevokeTokenAsync(string accessToken, CancellationToken ct = default)
    {
        var tokenHash = HashString(accessToken);
        var token = await _context.BCOAuthTokens
            .FirstOrDefaultAsync(t => t.AccessTokenHash == tokenHash && !t.IsRevoked, ct);

        if (token == null) return false;

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = "Manual revocation";

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Guid?> ValidateTokenAsync(string accessToken, string? requiredScope = null, CancellationToken ct = default)
    {
        var tokenHash = HashString(accessToken);
        var token = await _context.BCOAuthTokens
            .Include(t => t.ApiCredential)
            .FirstOrDefaultAsync(t => t.AccessTokenHash == tokenHash && !t.IsRevoked, ct);

        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            return null;

        if (token.ApiCredential == null || !token.ApiCredential.IsActive)
            return null;

        if (!string.IsNullOrEmpty(requiredScope))
        {
            var tokenScopes = token.Scopes.Split(',');
            if (!tokenScopes.Contains(requiredScope))
                return null;
        }

        return token.ApiCredential.BusinessConsumerId;
    }

    // ========== RATE LIMITING ==========

    public async Task<RateLimitStatusDto?> GetRateLimitStatusAsync(Guid apiCredentialId, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiCredentialId }, ct);
        if (credential == null) return null;

        var now = DateTime.UtcNow;
        var minuteKey = now.ToString("yyyy-MM-dd-HH:mm");
        var dayKey = now.ToString("yyyy-MM-dd");

        var minuteEntry = await _context.ApiRateLimitEntries
            .FirstOrDefaultAsync(e => e.ApiCredentialId == apiCredentialId && e.WindowKey == minuteKey, ct);

        var dayEntry = await _context.ApiRateLimitEntries
            .FirstOrDefaultAsync(e => e.ApiCredentialId == apiCredentialId && e.WindowKey == dayKey, ct);

        return new RateLimitStatusDto
        {
            RequestsThisMinute = minuteEntry?.RequestCount ?? 0,
            RequestsToday = dayEntry?.RequestCount ?? 0,
            MinuteLimit = credential.RateLimitPerMinute,
            DailyLimit = credential.RateLimitPerDay,
            RemainingMinute = credential.RateLimitPerMinute - (minuteEntry?.RequestCount ?? 0),
            RemainingDaily = credential.RateLimitPerDay - (dayEntry?.RequestCount ?? 0),
            MinuteResetAt = now.AddMinutes(1).Date.AddHours(now.Hour).AddMinutes(now.Minute + 1),
            DailyResetAt = now.Date.AddDays(1)
        };
    }

    public async Task<bool> CheckRateLimitAsync(Guid apiCredentialId, CancellationToken ct = default)
    {
        var status = await GetRateLimitStatusAsync(apiCredentialId, ct);
        if (status == null) return false;

        return status.RemainingMinute > 0 && status.RemainingDaily > 0;
    }

    public async Task IncrementRateLimitAsync(Guid apiCredentialId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var minuteKey = now.ToString("yyyy-MM-dd-HH:mm");
        var dayKey = now.ToString("yyyy-MM-dd");

        // Increment minute counter
        var minuteEntry = await _context.ApiRateLimitEntries
            .FirstOrDefaultAsync(e => e.ApiCredentialId == apiCredentialId && e.WindowKey == minuteKey, ct);

        if (minuteEntry == null)
        {
            minuteEntry = new ApiRateLimitEntry
            {
                Id = Guid.NewGuid(),
                ApiCredentialId = apiCredentialId,
                WindowKey = minuteKey,
                RequestCount = 1,
                WindowStart = now,
                ExpiresAt = now.AddMinutes(2)
            };
            _context.ApiRateLimitEntries.Add(minuteEntry);
        }
        else
        {
            minuteEntry.RequestCount++;
        }

        // Increment day counter
        var dayEntry = await _context.ApiRateLimitEntries
            .FirstOrDefaultAsync(e => e.ApiCredentialId == apiCredentialId && e.WindowKey == dayKey, ct);

        if (dayEntry == null)
        {
            dayEntry = new ApiRateLimitEntry
            {
                Id = Guid.NewGuid(),
                ApiCredentialId = apiCredentialId,
                WindowKey = dayKey,
                RequestCount = 1,
                WindowStart = now.Date,
                ExpiresAt = now.Date.AddDays(2)
            };
            _context.ApiRateLimitEntries.Add(dayEntry);
        }
        else
        {
            dayEntry.RequestCount++;
        }

        await _context.SaveChangesAsync(ct);
    }

    // ========== API USAGE LOGGING ==========

    public async Task LogApiUsageAsync(Guid apiCredentialId, string endpoint, string httpMethod, int statusCode, int responseTimeMs, string? requestIp, string? userAgent, string? errorMessage = null, CancellationToken ct = default)
    {
        var log = new ApiKeyUsageLog
        {
            Id = Guid.NewGuid(),
            ApiCredentialId = apiCredentialId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            ResponseStatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            RequestIp = requestIp,
            UserAgent = userAgent,
            ErrorMessage = errorMessage,
            RequestedAt = DateTime.UtcNow
        };

        _context.ApiKeyUsageLogs.Add(log);

        // Update last used on credential
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiCredentialId }, ct);
        if (credential != null)
        {
            credential.LastUsedAt = DateTime.UtcNow;
            credential.LastUsedIp = requestIp;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<ApiUsageStatsDto> GetApiUsageStatsAsync(Guid apiCredentialId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var credential = await _context.BCApiCredentials.FindAsync(new object[] { apiCredentialId }, ct);

        fromDate ??= DateTime.UtcNow.AddDays(-30);
        toDate ??= DateTime.UtcNow;

        var logs = await _context.ApiKeyUsageLogs
            .Where(l => l.ApiCredentialId == apiCredentialId)
            .Where(l => l.RequestedAt >= fromDate && l.RequestedAt <= toDate)
            .ToListAsync(ct);

        return new ApiUsageStatsDto
        {
            ApiCredentialId = apiCredentialId,
            ApiKeyId = credential?.ApiKeyId ?? "",
            TotalRequests = logs.Count,
            SuccessfulRequests = logs.Count(l => l.ResponseStatusCode >= 200 && l.ResponseStatusCode < 300),
            FailedRequests = logs.Count(l => l.ResponseStatusCode >= 400),
            AverageResponseTimeMs = logs.Any() ? logs.Average(l => l.ResponseTimeMs) : 0,
            RequestsByEndpoint = logs.GroupBy(l => l.Endpoint).ToDictionary(g => g.Key, g => g.Count()),
            RequestsByStatus = logs.GroupBy(l => l.ResponseStatusCode).ToDictionary(g => g.Key, g => g.Count()),
            PeriodStart = fromDate.Value,
            PeriodEnd = toDate.Value
        };
    }

    // ========== WEBHOOK MANAGEMENT ==========

    public async Task<WebhookDto> CreateWebhookAsync(Guid businessConsumerId, CreateWebhookRequest request, CancellationToken ct = default)
    {
        var secret = GenerateRandomString(32);

        var webhook = new BCWebhook
        {
            Id = Guid.NewGuid(),
            BusinessConsumerId = businessConsumerId,
            WebhookUrl = request.WebhookUrl,
            Events = string.Join(",", request.Events),
            Secret = HashString(secret),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BCWebhooks.Add(webhook);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created webhook {WebhookId} for BC {BusinessConsumerId}", webhook.Id, businessConsumerId);

        return new WebhookDto
        {
            Id = webhook.Id,
            WebhookUrl = webhook.WebhookUrl,
            Events = webhook.Events.Split(',').ToList(),
            Secret = secret, // Only returned on creation
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt
        };
    }

    public async Task<List<WebhookDto>> GetWebhooksAsync(Guid businessConsumerId, CancellationToken ct = default)
    {
        var webhooks = await _context.BCWebhooks
            .Where(w => w.BusinessConsumerId == businessConsumerId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        return webhooks.Select(w => new WebhookDto
        {
            Id = w.Id,
            WebhookUrl = w.WebhookUrl,
            Events = w.Events.Split(',').ToList(),
            IsActive = w.IsActive,
            FailureCount = w.FailureCount,
            LastTriggeredAt = w.LastTriggeredAt,
            LastSuccessAt = w.LastSuccessAt,
            LastErrorMessage = w.LastErrorMessage,
            CreatedAt = w.CreatedAt
        }).ToList();
    }

    public async Task<WebhookDto?> GetWebhookAsync(Guid webhookId, CancellationToken ct = default)
    {
        var webhook = await _context.BCWebhooks.FindAsync(new object[] { webhookId }, ct);
        if (webhook == null) return null;

        return new WebhookDto
        {
            Id = webhook.Id,
            WebhookUrl = webhook.WebhookUrl,
            Events = webhook.Events.Split(',').ToList(),
            IsActive = webhook.IsActive,
            FailureCount = webhook.FailureCount,
            LastTriggeredAt = webhook.LastTriggeredAt,
            LastSuccessAt = webhook.LastSuccessAt,
            LastErrorMessage = webhook.LastErrorMessage,
            CreatedAt = webhook.CreatedAt
        };
    }

    public async Task<bool> UpdateWebhookAsync(Guid webhookId, UpdateWebhookRequest request, CancellationToken ct = default)
    {
        var webhook = await _context.BCWebhooks.FindAsync(new object[] { webhookId }, ct);
        if (webhook == null) return false;

        if (!string.IsNullOrEmpty(request.WebhookUrl))
            webhook.WebhookUrl = request.WebhookUrl;
        if (request.Events != null && request.Events.Any())
            webhook.Events = string.Join(",", request.Events);
        if (request.IsActive.HasValue)
            webhook.IsActive = request.IsActive.Value;

        webhook.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken ct = default)
    {
        var webhook = await _context.BCWebhooks.FindAsync(new object[] { webhookId }, ct);
        if (webhook == null) return false;

        _context.BCWebhooks.Remove(webhook);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> TestWebhookAsync(Guid webhookId, CancellationToken ct = default)
    {
        var webhook = await _context.BCWebhooks.FindAsync(new object[] { webhookId }, ct);
        if (webhook == null) return false;

        var testPayload = new WebhookEventPayload
        {
            EventType = "webhook.test",
            Timestamp = DateTime.UtcNow,
            Data = new { message = "This is a test webhook delivery" }
        };

        return await DeliverWebhookAsync(webhook, testPayload, ct);
    }

    public async Task TriggerWebhooksAsync(Guid businessConsumerId, string eventType, object eventData, CancellationToken ct = default)
    {
        var webhooks = await _context.BCWebhooks
            .Where(w => w.BusinessConsumerId == businessConsumerId && w.IsActive)
            .Where(w => w.Events.Contains(eventType))
            .ToListAsync(ct);

        var payload = new WebhookEventPayload
        {
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Data = eventData
        };

        foreach (var webhook in webhooks)
        {
            _ = DeliverWebhookAsync(webhook, payload, ct); // Fire and forget
        }
    }

    private async Task<bool> DeliverWebhookAsync(BCWebhook webhook, WebhookEventPayload payload, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        var log = new WebhookDeliveryLog
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            EventType = payload.EventType,
            Payload = JsonSerializer.Serialize(payload),
            AttemptNumber = 1,
            AttemptedAt = startTime
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            // Add signature header if secret exists
            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = ComputeHmacSha256(log.Payload, webhook.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }

            var response = await client.PostAsync(webhook.WebhookUrl, content, ct);

            log.HttpStatusCode = (int)response.StatusCode;
            log.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            log.ResponseBody = await response.Content.ReadAsStringAsync(ct);
            log.IsSuccess = response.IsSuccessStatusCode;

            webhook.LastTriggeredAt = DateTime.UtcNow;
            if (log.IsSuccess)
            {
                webhook.LastSuccessAt = DateTime.UtcNow;
                webhook.FailureCount = 0;
            }
            else
            {
                webhook.FailureCount++;
                webhook.LastErrorMessage = $"HTTP {log.HttpStatusCode}";
            }
        }
        catch (Exception ex)
        {
            log.HttpStatusCode = 0;
            log.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;

            webhook.FailureCount++;
            webhook.LastErrorMessage = ex.Message;
            webhook.LastTriggeredAt = DateTime.UtcNow;

            _logger.LogWarning(ex, "Webhook delivery failed for {WebhookId}", webhook.Id);
        }

        _context.WebhookDeliveryLogs.Add(log);
        webhook.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return log.IsSuccess;
    }

    // ========== HELPER METHODS ==========

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    private static string HashString(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHmacSha256(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLower();
    }
}
