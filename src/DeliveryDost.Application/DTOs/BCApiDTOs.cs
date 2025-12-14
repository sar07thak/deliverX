using System;
using System.Collections.Generic;

namespace DeliveryDost.Application.DTOs.BCApi;

// API Credential DTOs
public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty; // "Production", "Testing", etc.
    public string Environment { get; set; } = "SANDBOX"; // SANDBOX, PRODUCTION
    public DateTime? ExpiresAt { get; set; }
    public int? RateLimitPerMinute { get; set; }
    public int? RateLimitPerDay { get; set; }
    public string? AllowedIps { get; set; }
    public List<string> Scopes { get; set; } = new();
}

public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public string ApiKeyId { get; set; } = string.Empty; // DDK_xxxx
    public string? ApiKeySecret { get; set; } // Only returned on creation
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RateLimitPerMinute { get; set; }
    public int RateLimitPerDay { get; set; }
    public string? AllowedIps { get; set; }
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyListDto
{
    public Guid Id { get; set; }
    public string ApiKeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RevokeApiKeyRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public int? RateLimitPerMinute { get; set; }
    public int? RateLimitPerDay { get; set; }
    public string? AllowedIps { get; set; }
    public List<string>? Scopes { get; set; }
}

// OAuth DTOs
public class TokenRequest
{
    public string GrantType { get; set; } = "client_credentials"; // client_credentials, refresh_token
    public string? ApiKeyId { get; set; }
    public string? ApiKeySecret { get; set; }
    public string? RefreshToken { get; set; }
    public string? Scope { get; set; }
}

public class TokenResponse
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // seconds
    public string? Scope { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}

// Webhook DTOs
public class CreateWebhookRequest
{
    public string WebhookUrl { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
}

public class WebhookDto
{
    public Guid Id { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public string? Secret { get; set; } // Only on creation
    public bool IsActive { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateWebhookRequest
{
    public string? WebhookUrl { get; set; }
    public List<string>? Events { get; set; }
    public bool? IsActive { get; set; }
}

public class WebhookEventPayload
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object Data { get; set; } = new();
}

// API Usage Stats
public class ApiUsageStatsDto
{
    public Guid ApiCredentialId { get; set; }
    public string ApiKeyId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public Dictionary<string, int> RequestsByEndpoint { get; set; } = new();
    public Dictionary<int, int> RequestsByStatus { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class RateLimitStatusDto
{
    public int RequestsThisMinute { get; set; }
    public int RequestsToday { get; set; }
    public int MinuteLimit { get; set; }
    public int DailyLimit { get; set; }
    public int RemainingMinute { get; set; }
    public int RemainingDaily { get; set; }
    public DateTime MinuteResetAt { get; set; }
    public DateTime DailyResetAt { get; set; }
}

// Available scopes
public static class ApiScopes
{
    public const string DeliveryRead = "delivery:read";
    public const string DeliveryCreate = "delivery:create";
    public const string DeliveryUpdate = "delivery:update";
    public const string DeliveryCancel = "delivery:cancel";
    public const string RatesRead = "rates:read";
    public const string TrackingRead = "tracking:read";
    public const string WebhooksManage = "webhooks:manage";
    public const string ReportsRead = "reports:read";

    public static readonly List<string> All = new()
    {
        DeliveryRead, DeliveryCreate, DeliveryUpdate, DeliveryCancel,
        RatesRead, TrackingRead, WebhooksManage, ReportsRead
    };
}

// Available webhook events
public static class WebhookEvents
{
    public const string DeliveryCreated = "delivery.created";
    public const string DeliveryPickedUp = "delivery.picked_up";
    public const string DeliveryInTransit = "delivery.in_transit";
    public const string DeliveryDelivered = "delivery.delivered";
    public const string DeliveryCancelled = "delivery.cancelled";
    public const string DeliveryFailed = "delivery.failed";
    public const string PaymentReceived = "payment.received";
    public const string InvoiceGenerated = "invoice.generated";

    public static readonly List<string> All = new()
    {
        DeliveryCreated, DeliveryPickedUp, DeliveryInTransit,
        DeliveryDelivered, DeliveryCancelled, DeliveryFailed,
        PaymentReceived, InvoiceGenerated
    };
}
