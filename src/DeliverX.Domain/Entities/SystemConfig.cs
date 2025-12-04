using System;

namespace DeliverX.Domain.Entities;

/// <summary>
/// System configuration key-value store
/// </summary>
public class SystemConfig
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // GENERAL, PRICING, COMMISSION, NOTIFICATIONS, etc.
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "STRING"; // STRING, NUMBER, BOOLEAN, JSON
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Admin action audit log (separate from AuthAuditLog)
/// </summary>
public class AdminAuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, APPROVE, REJECT, etc.
    public string? EntityType { get; set; } // USER, KYC, CONFIG, etc.
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
