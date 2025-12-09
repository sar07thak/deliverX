using Microsoft.Extensions.Logging;
using DeliveryDost.Application.DTOs.Registration;

namespace DeliveryDost.Infrastructure.Services.External;

public interface IDigiLockerClient
{
    Task<string> GetAuthorizationUrlAsync(string userId, string redirectUrl);
    Task<AadhaarDataDto> GetAadhaarDataAsync(string code);
}

/// <summary>
/// Mock DigiLocker client for MVP
/// In production, replace with actual DigiLocker API integration
/// </summary>
public class MockDigiLockerClient : IDigiLockerClient
{
    private readonly ILogger<MockDigiLockerClient> _logger;
    private static readonly Dictionary<string, AadhaarDataDto> _mockData = new();

    public MockDigiLockerClient(ILogger<MockDigiLockerClient> logger)
    {
        _logger = logger;
    }

    public Task<string> GetAuthorizationUrlAsync(string userId, string redirectUrl)
    {
        _logger.LogInformation("Mock DigiLocker: Generating authorization URL for user {UserId}", userId);

        // Generate a fake authorization code
        var authCode = Guid.NewGuid().ToString();

        // Store mock Aadhaar data for this code
        _mockData[authCode] = new AadhaarDataDto
        {
            AadhaarNumber = GenerateMockAadhaarNumber(),
            Name = GenerateMockName(),
            DOB = DateTime.UtcNow.AddYears(-25).AddDays(Random.Shared.Next(-3650, 0)),
            Gender = Random.Shared.Next(2) == 0 ? "Male" : "Female",
            Address = new AddressDto
            {
                Line1 = "Mock Address Line 1",
                City = "Jaipur",
                State = "Rajasthan",
                Pincode = "302001"
            },
            ReferenceId = $"UIDAI-REF-{Guid.NewGuid().ToString()[..8].ToUpper()}"
        };

        // In a real implementation, this would be DigiLocker's OAuth URL
        var mockUrl = $"https://mock-digilocker.gov.in/auth?code={authCode}&redirect_uri={Uri.EscapeDataString(redirectUrl)}";

        return Task.FromResult(mockUrl);
    }

    public Task<AadhaarDataDto> GetAadhaarDataAsync(string code)
    {
        _logger.LogInformation("Mock DigiLocker: Fetching Aadhaar data for code {Code}", code);

        if (_mockData.TryGetValue(code, out var data))
        {
            return Task.FromResult(data);
        }

        // If code not found, generate fresh mock data
        var mockData = new AadhaarDataDto
        {
            AadhaarNumber = GenerateMockAadhaarNumber(),
            Name = GenerateMockName(),
            DOB = DateTime.UtcNow.AddYears(-30),
            Gender = "Male",
            Address = new AddressDto
            {
                Line1 = "123 Mock Street",
                City = "Jaipur",
                State = "Rajasthan",
                Pincode = "302001"
            },
            ReferenceId = $"UIDAI-REF-{Guid.NewGuid().ToString()[..8].ToUpper()}"
        };

        return Task.FromResult(mockData);
    }

    private static string GenerateMockAadhaarNumber()
    {
        // Generate a 12-digit number
        return Random.Shared.NextInt64(100000000000, 999999999999).ToString();
    }

    private static string GenerateMockName()
    {
        var firstNames = new[] { "Ravi", "Priya", "Amit", "Sneha", "Rahul", "Anjali", "Vikram", "Pooja" };
        var lastNames = new[] { "Kumar", "Sharma", "Singh", "Patel", "Gupta", "Reddy", "Verma", "Shah" };

        return $"{firstNames[Random.Shared.Next(firstNames.Length)]} {lastNames[Random.Shared.Next(lastNames.Length)]}";
    }
}
