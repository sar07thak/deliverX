using System.Text.Json;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAuthEventAsync(
        string eventType,
        Guid? userId = null,
        string? phone = null,
        string? email = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuthAuditLog
        {
            EventType = eventType,
            UserId = userId,
            Phone = phone,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuthAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
