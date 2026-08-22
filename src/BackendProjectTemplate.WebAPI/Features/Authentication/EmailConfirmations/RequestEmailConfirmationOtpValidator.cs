using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.EmailConfirmations;

public sealed class RequestEmailConfirmationOtpValidator : AbstractValidator<RequestEmailConfirmationOtpRequest>
{
    public RequestEmailConfirmationOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
