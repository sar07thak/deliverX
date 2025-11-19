using DeliverX.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace DeliverX.Tests.Services;

public class TotpServiceTests
{
    private readonly TotpService _totpService;

    public TotpServiceTests()
    {
        _totpService = new TotpService();
    }

    [Fact]
    public void GenerateSecret_ShouldReturnNonEmptyString()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Assert
        secret.Should().NotBeNullOrEmpty();
        secret.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSecret_ShouldReturnDifferentSecretsEachTime()
    {
        // Act
        var secret1 = _totpService.GenerateSecret();
        var secret2 = _totpService.GenerateSecret();

        // Assert
        secret1.Should().NotBe(secret2);
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldReturnValidOtpauthUri()
    {
        // Arrange
        var email = "test@example.com";
        var secret = _totpService.GenerateSecret();
        var issuer = "DeliverX";

        // Act
        var uri = _totpService.GenerateQrCodeUri(email, secret, issuer);

        // Assert
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain(issuer);
        uri.Should().Contain("secret=");
    }

    [Fact]
    public void VerifyCode_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var invalidCode = "000000";

        // Act
        var result = _totpService.VerifyCode(secret, invalidCode);

        // Assert - highly unlikely to be valid
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithEmptySecretOrCode_ShouldReturnFalse()
    {
        // Act & Assert
        _totpService.VerifyCode("", "123456").Should().BeFalse();
        _totpService.VerifyCode("secret", "").Should().BeFalse();
    }
}
