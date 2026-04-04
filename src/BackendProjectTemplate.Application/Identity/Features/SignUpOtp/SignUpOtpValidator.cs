using FluentValidation;

namespace BackendProjectTemplate.Application.Identity.Features.SignUpOtp;

public sealed class SignUpOtpValidator : AbstractValidator<SignUpOtpRequest>
{
    public SignUpOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Otp)
            .NotEmpty()
            .Length(6);
    }
}
