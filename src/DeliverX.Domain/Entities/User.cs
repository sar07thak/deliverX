namespace DeliverX.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = string.Empty; // DP, DPCM, DBC, EC, Inspector, SuperAdmin
    public bool Is2FAEnabled { get; set; } = false;
    public string? TotpSecret { get; set; } // Encrypted
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
