using FluentValidation;
using DeliverX.Application.DTOs.Auth;

namespace DeliverX.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

        RuleFor(x => x.TotpCode)
            .Matches(@"^\d{6}$").WithMessage("2FA code must be exactly 6 digits")
            .When(x => !string.IsNullOrEmpty(x.TotpCode));
    }
}
