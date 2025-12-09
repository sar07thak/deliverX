using FluentValidation;
using DeliveryDost.Application.DTOs.Auth;

namespace DeliveryDost.Application.Validators;

public class OtpVerifyRequestValidator : AbstractValidator<OtpVerifyRequest>
{
    public OtpVerifyRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required")
            .Matches(@"^\d{6}$").WithMessage("OTP must be exactly 6 digits");
    }
}
