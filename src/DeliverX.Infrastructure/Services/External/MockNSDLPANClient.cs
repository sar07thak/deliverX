using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using DeliverX.Application.DTOs.Registration;

namespace DeliverX.Infrastructure.Services.External;

public interface INSDLPANClient
{
    Task<PANDetailsDto?> VerifyPANAsync(string pan);
}

/// <summary>
/// Mock NSDL PAN verification client for MVP
/// In production, replace with actual NSDL PAN API integration
/// </summary>
public class MockNSDLPANClient : INSDLPANClient
{
    private readonly ILogger<MockNSDLPANClient> _logger;

    public MockNSDLPANClient(ILogger<MockNSDLPANClient> logger)
    {
        _logger = logger;
    }

    public Task<PANDetailsDto?> VerifyPANAsync(string pan)
    {
        _logger.LogInformation("Mock NSDL: Verifying PAN {PAN}", pan);

        // Validate PAN format
        if (!Regex.IsMatch(pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"))
        {
            _logger.LogWarning("Mock NSDL: Invalid PAN format {PAN}", pan);
            return Task.FromResult<PANDetailsDto?>(null);
        }

        // Simulate API delay
        Thread.Sleep(500);

        // Mock PAN details
        var mockDetails = new PANDetailsDto
        {
            PAN = pan,
            Name = GenerateNameFromPAN(pan),
            DOB = DateTime.UtcNow.AddYears(-30).AddDays(Random.Shared.Next(-3650, 0)),
            Status = "ACTIVE"
        };

        _logger.LogInformation("Mock NSDL: PAN verified successfully {PAN}", pan);
        return Task.FromResult<PANDetailsDto?>(mockDetails);
    }

    private static string GenerateNameFromPAN(string pan)
    {
        // In mock mode, derive a name from the PAN
        // First 3 letters could be initials, 4th letter could be surname initial
        var firstNames = new[] { "RAVI", "PRIYA", "AMIT", "SNEHA", "RAHUL", "ANJALI", "VIKRAM", "POOJA" };
        var lastNames = new[] { "KUMAR", "SHARMA", "SINGH", "PATEL", "GUPTA", "REDDY", "VERMA", "SHAH" };

        var firstName = firstNames[Math.Abs(pan[0] + pan[1]) % firstNames.Length];
        var lastName = lastNames[Math.Abs(pan[3] + pan[4]) % lastNames.Length];

        return $"{firstName} {lastName}";
    }
}
