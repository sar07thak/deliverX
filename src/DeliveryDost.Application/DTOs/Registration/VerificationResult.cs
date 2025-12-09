namespace DeliveryDost.Application.DTOs.Registration;

public class VerificationResult
{
    public bool IsSuccess { get; set; }
    public Guid? KycId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public object? VerifiedData { get; set; }
    public int? NameMatchScore { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public static VerificationResult Success(object? data = null, string? message = null)
    {
        return new VerificationResult
        {
            IsSuccess = true,
            Status = "VERIFIED",
            Message = message ?? "Verification successful",
            VerifiedData = data,
            VerifiedAt = DateTime.UtcNow
        };
    }

    public static VerificationResult Failure(string errorCode, string errorMessage)
    {
        return new VerificationResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public static VerificationResult Pending(Guid kycId, string message)
    {
        return new VerificationResult
        {
            IsSuccess = true,
            KycId = kycId,
            Status = "PENDING",
            Message = message
        };
    }
}

public class DuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public List<string> DuplicateFields { get; set; } = new();
    public Guid? ExistingUserId { get; set; }
    public List<string> Warnings { get; set; } = new();
}
