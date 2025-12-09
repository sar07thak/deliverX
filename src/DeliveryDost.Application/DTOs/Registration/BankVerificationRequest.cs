namespace DeliveryDost.Application.DTOs.Registration;

public class BankVerificationRequest
{
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string IFSCCode { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Method { get; set; } = "PENNY_DROP"; // PENNY_DROP, NPCI
}

public class BankDetailsDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string IFSCCode { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
}
