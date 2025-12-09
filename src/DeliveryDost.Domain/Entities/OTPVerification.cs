namespace DeliveryDost.Domain.Entities;

public class OTPVerification
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string OTPHash { get; set; } = string.Empty;
    public int Attempts { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; } = false;
}
