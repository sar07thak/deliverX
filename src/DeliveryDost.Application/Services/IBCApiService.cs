using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeliveryDost.Application.DTOs.BCApi;

namespace DeliveryDost.Application.Services;

public interface IBCApiService
{
    // API Key Management
    Task<ApiKeyResponse> CreateApiKeyAsync(Guid businessConsumerId, CreateApiKeyRequest request, CancellationToken ct = default);
    Task<List<ApiKeyListDto>> GetApiKeysAsync(Guid businessConsumerId, CancellationToken ct = default);
    Task<ApiKeyResponse?> GetApiKeyAsync(Guid apiKeyId, CancellationToken ct = default);
    Task<bool> UpdateApiKeyAsync(Guid apiKeyId, UpdateApiKeyRequest request, CancellationToken ct = default);
    Task<bool> RevokeApiKeyAsync(Guid apiKeyId, RevokeApiKeyRequest request, CancellationToken ct = default);
    Task<bool> RegenerateApiKeySecretAsync(Guid apiKeyId, CancellationToken ct = default);

    // OAuth Token Management
    Task<TokenResponse> GenerateTokenAsync(TokenRequest request, CancellationToken ct = default);
    Task<bool> RevokeTokenAsync(string accessToken, CancellationToken ct = default);
    Task<Guid?> ValidateTokenAsync(string accessToken, string? requiredScope = null, CancellationToken ct = default);

    // Rate Limiting
    Task<RateLimitStatusDto?> GetRateLimitStatusAsync(Guid apiCredentialId, CancellationToken ct = default);
    Task<bool> CheckRateLimitAsync(Guid apiCredentialId, CancellationToken ct = default);
    Task IncrementRateLimitAsync(Guid apiCredentialId, CancellationToken ct = default);

    // API Usage Logging & Analytics
    Task LogApiUsageAsync(Guid apiCredentialId, string endpoint, string httpMethod, int statusCode, int responseTimeMs, string? requestIp, string? userAgent, string? errorMessage = null, CancellationToken ct = default);
    Task<ApiUsageStatsDto> GetApiUsageStatsAsync(Guid apiCredentialId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);

    // Webhook Management
    Task<WebhookDto> CreateWebhookAsync(Guid businessConsumerId, CreateWebhookRequest request, CancellationToken ct = default);
    Task<List<WebhookDto>> GetWebhooksAsync(Guid businessConsumerId, CancellationToken ct = default);
    Task<WebhookDto?> GetWebhookAsync(Guid webhookId, CancellationToken ct = default);
    Task<bool> UpdateWebhookAsync(Guid webhookId, UpdateWebhookRequest request, CancellationToken ct = default);
    Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken ct = default);
    Task<bool> TestWebhookAsync(Guid webhookId, CancellationToken ct = default);

    // Webhook Delivery
    Task TriggerWebhooksAsync(Guid businessConsumerId, string eventType, object eventData, CancellationToken ct = default);
}
