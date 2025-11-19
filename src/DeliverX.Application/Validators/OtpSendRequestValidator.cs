using FluentValidation;
using DeliverX.Application.DTOs.Auth;

namespace DeliverX.Application.Validators;

public class OtpSendRequestValidator : AbstractValidator<OtpSendRequest>
{
    public OtpSendRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required")
            .Matches(@"^\+\d{1,3}$").WithMessage("Country code must start with + and have 1-3 digits");
    }
}
