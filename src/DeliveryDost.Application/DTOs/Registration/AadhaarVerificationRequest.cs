namespace DeliveryDost.Application.DTOs.Registration;

public class AadhaarVerificationRequest
{
    public Guid UserId { get; set; }
    public string? AadhaarLast4 { get; set; }
    public string Method { get; set; } = "DIGILOCKER"; // MANUAL_UPLOAD, DIGILOCKER
    public string? DocumentUrl { get; set; } // For manual upload
    public string? RedirectUrl { get; set; } // For DigiLocker
}

public class AadhaarVerificationCallback
{
    public Guid UserId { get; set; }
    public Guid KycId { get; set; }
    public string? DigilockerToken { get; set; }
    public string? AadhaarXml { get; set; } // Base64-encoded XML
}

public class AadhaarDataDto
{
    public string AadhaarNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public string Gender { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
    public string ReferenceId { get; set; } = string.Empty;
}
