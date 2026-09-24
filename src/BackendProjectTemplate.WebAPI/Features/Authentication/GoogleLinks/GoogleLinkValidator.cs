using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.GoogleLinks;

public sealed class GoogleLinkValidator : AbstractValidator<GoogleLinkRequest>
{
    public GoogleLinkValidator()
    {
        RuleFor(request => request.FlowToken).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}
