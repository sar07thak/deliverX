namespace DeliverX.Application.DTOs.Auth;

public class SessionDto
{
    public Guid Id { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime LastActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCurrent { get; set; }
}

public class SessionListResponse
{
    public List<SessionDto> Sessions { get; set; } = new();
}
