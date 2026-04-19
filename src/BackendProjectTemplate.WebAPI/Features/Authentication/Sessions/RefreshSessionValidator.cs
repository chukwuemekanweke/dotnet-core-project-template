using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;

public sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionRequest>
{
    public RefreshSessionValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
