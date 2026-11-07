using FluentValidation;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed class CompletePasskeyValidator : AbstractValidator<CompletePasskeyRequest>
{
    public CompletePasskeyValidator()
    {
        RuleFor(request => request.FlowToken).NotEmpty().MaximumLength(500);
        RuleFor(request => request.Credential)
            .Must(credential => credential.ValueKind == JsonValueKind.Object)
            .WithMessage("Credential must be a JSON object.");
    }
}
