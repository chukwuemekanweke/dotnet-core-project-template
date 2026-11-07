using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed class CreatePasskeyRegistrationOptionsValidator : AbstractValidator<CreatePasskeyRegistrationOptionsRequest>
{
    public CreatePasskeyRegistrationOptionsValidator() =>
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
}
