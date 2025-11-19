namespace DeliverX.Application.Services;

public interface IAuditService
{
    /// <summary>
    /// Log an authentication event
    /// </summary>
    Task LogAuthEventAsync(
        string eventType,
        Guid? userId = null,
        string? phone = null,
        string? email = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}
