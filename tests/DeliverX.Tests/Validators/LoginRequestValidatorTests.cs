using DeliverX.Application.DTOs.Auth;
using DeliverX.Application.Validators;
using FluentAssertions;
using Xunit;

namespace DeliverX.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "StrongPassword123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "StrongPassword123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "invalid-email",
            Password = "StrongPassword123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Short1!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_With2FACode_ShouldValidateFormat()
    {
        // Arrange - invalid 2FA code (not 6 digits)
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "StrongPassword123!",
            TotpCode = "123" // Invalid: not 6 digits
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public void Validate_WithValid2FACode_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "StrongPassword123!",
            TotpCode = "123456" // Valid: 6 digits
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
