using System;
using System.Collections.Generic;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// API credentials for Business Consumers to integrate with DeliveryDost
/// </summary>
public class BCApiCredential
{
    public Guid Id { get; set; }
    public Guid BusinessConsumerId { get; set; }
    public string ApiKeyId { get; set; } = string.Empty; // Public identifier (DDK_xxxx)
    public string ApiKeyHash { get; set; } = string.Empty; // Hashed secret
    public string Name { get; set; } = string.Empty; // "Production", "Testing", etc.
    public string Environment { get; set; } = "SANDBOX"; // SANDBOX, PRODUCTION
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public int RateLimitPerMinute { get; set; } = 60;
    public int RateLimitPerDay { get; set; } = 10000;
    public string? AllowedIps { get; set; } // Comma-separated IPs for whitelist
    public string Scopes { get; set; } = "delivery:read,delivery:create"; // Comma-separated permissions
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BusinessConsumerProfile? BusinessConsumer { get; set; }
    public ICollection<ApiKeyUsageLog> UsageLogs { get; set; } = new List<ApiKeyUsageLog>();
}

/// <summary>
/// OAuth2 token for BC API authentication
/// </summary>
public class BCOAuthToken
{
    public Guid Id { get; set; }
    public Guid ApiCredentialId { get; set; }
    public string AccessTokenHash { get; set; } = string.Empty;
    public string? RefreshTokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public string? IssuedToIp { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BCApiCredential? ApiCredential { get; set; }
}

/// <summary>
/// API usage log for rate limiting and analytics
/// </summary>
public class ApiKeyUsageLog
{
    public Guid Id { get; set; }
    public Guid ApiCredentialId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? RequestIp { get; set; }
    public string? UserAgent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BCApiCredential? ApiCredential { get; set; }
}

/// <summary>
/// Webhook configuration for BC notifications
/// </summary>
public class BCWebhook
{
    public Guid Id { get; set; }
    public Guid BusinessConsumerId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string Events { get; set; } = string.Empty; // Comma-separated: delivery.created,delivery.picked_up,delivery.delivered
    public string? Secret { get; set; } // For HMAC signature verification
    public bool IsActive { get; set; } = true;
    public int FailureCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BusinessConsumerProfile? BusinessConsumer { get; set; }
    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}

/// <summary>
/// Log of webhook delivery attempts
/// </summary>
public class WebhookDeliveryLog
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int ResponseTimeMs { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BCWebhook? Webhook { get; set; }
}

/// <summary>
/// API rate limit tracking (for Redis-backed rate limiting)
/// </summary>
public class ApiRateLimitEntry
{
    public Guid Id { get; set; }
    public Guid ApiCredentialId { get; set; }
    public string WindowKey { get; set; } = string.Empty; // e.g., "2024-01-15-14:30" for minute window
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime ExpiresAt { get; set; }
}
