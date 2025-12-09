namespace DeliveryDost.Application.DTOs.Common;

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
    public int? RetryAfter { get; set; }
    public int? AttemptsRemaining { get; set; }
}
