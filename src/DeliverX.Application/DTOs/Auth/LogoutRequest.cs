namespace DeliverX.Application.DTOs.Auth;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public bool LogoutAll { get; set; } = false;
}
