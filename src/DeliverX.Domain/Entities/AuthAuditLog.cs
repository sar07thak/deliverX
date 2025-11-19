namespace DeliverX.Domain.Entities;

public class AuthAuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty; // LOGIN_SUCCESS, LOGIN_FAILED, OTP_SENT, SESSION_REVOKED, etc.
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; } // JSON for additional info
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}
