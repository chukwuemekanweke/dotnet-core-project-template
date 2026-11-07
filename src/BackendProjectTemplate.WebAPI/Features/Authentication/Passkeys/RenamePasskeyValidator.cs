using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed class RenamePasskeyValidator : AbstractValidator<RenamePasskeyRequest>
{
    public RenamePasskeyValidator() =>
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
}
