using Microsoft.Extensions.Logging;
using DeliverX.Application.DTOs.Registration;

namespace DeliverX.Infrastructure.Services.External;

public interface IBankVerificationClient
{
    Task<BankVerificationResultDto> VerifyBankAccountAsync(string accountNumber, string ifscCode, string accountHolderName, string method);
}

public class BankVerificationResultDto
{
    public bool IsSuccess { get; set; }
    public string? TransactionId { get; set; }
    public BankDetailsDto? BankDetails { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mock Bank Verification client for MVP
/// In production, replace with actual Razorpay/Cashfree/NPCI API integration
/// </summary>
public class MockBankVerificationClient : IBankVerificationClient
{
    private readonly ILogger<MockBankVerificationClient> _logger;

    public MockBankVerificationClient(ILogger<MockBankVerificationClient> logger)
    {
        _logger = logger;
    }

    public async Task<BankVerificationResultDto> VerifyBankAccountAsync(
        string accountNumber,
        string ifscCode,
        string accountHolderName,
        string method)
    {
        _logger.LogInformation("Mock Bank API: Verifying account {AccountNumber} with IFSC {IFSC} using {Method}",
            MaskAccountNumber(accountNumber), ifscCode, method);

        // Simulate penny drop delay (2-5 seconds in real world)
        await Task.Delay(2000);

        // Validate basic format
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 9 || accountNumber.Length > 18)
        {
            return new BankVerificationResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Invalid account number format"
            };
        }

        if (string.IsNullOrEmpty(ifscCode) || ifscCode.Length != 11)
        {
            return new BankVerificationResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Invalid IFSC code format"
            };
        }

        // Mock successful verification
        var bankDetails = new BankDetailsDto
        {
            AccountNumber = accountNumber,
            IFSCCode = ifscCode,
            AccountHolderName = accountHolderName.ToUpper(),
            BankName = GetBankNameFromIFSC(ifscCode),
            BranchName = GetBranchNameFromIFSC(ifscCode)
        };

        var result = new BankVerificationResultDto
        {
            IsSuccess = true,
            TransactionId = $"TXN-{Guid.NewGuid().ToString()[..12].ToUpper()}",
            BankDetails = bankDetails
        };

        _logger.LogInformation("Mock Bank API: Verification successful for account {AccountNumber}",
            MaskAccountNumber(accountNumber));

        return result;
    }

    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return "****";

        return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
    }

    private static string GetBankNameFromIFSC(string ifsc)
    {
        // First 4 characters identify the bank
        var bankCode = ifsc[..4];

        return bankCode switch
        {
            "SBIN" => "State Bank of India",
            "HDFC" => "HDFC Bank",
            "ICIC" => "ICICI Bank",
            "AXIS" => "Axis Bank",
            "PUNB" => "Punjab National Bank",
            "UTIB" => "Axis Bank",
            "KKBK" => "Kotak Mahindra Bank",
            "IDIB" => "Indian Bank",
            _ => "Mock Bank Limited"
        };
    }

    private static string GetBranchNameFromIFSC(string ifsc)
    {
        // In mock mode, generate a branch name
        var cities = new[] { "Jaipur", "Delhi", "Mumbai", "Bangalore", "Hyderabad", "Chennai" };
        var areas = new[] { "Main Branch", "Sector 21", "MG Road", "City Center", "Old Town" };

        var cityIndex = Math.Abs(ifsc[5] + ifsc[6]) % cities.Length;
        var areaIndex = Math.Abs(ifsc[8] + ifsc[9]) % areas.Length;

        return $"{cities[cityIndex]} - {areas[areaIndex]}";
    }
}
