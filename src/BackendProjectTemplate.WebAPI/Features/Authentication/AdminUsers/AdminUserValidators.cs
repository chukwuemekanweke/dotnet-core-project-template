using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;

public sealed class ListAdminUsersValidator : AbstractValidator<ListAdminUsersRequest>
{
    public ListAdminUsersValidator()
    {
        RuleFor(request => request.Search).MaximumLength(200);
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Role)
            .Must(role => role is null || ApplicationRoles.All.Contains(role, StringComparer.Ordinal))
            .WithMessage("Role must be one of the known application roles.");
    }
}

public sealed class AdminSecurityMutationValidator : AbstractValidator<AdminSecurityMutationRequest>
{
    public AdminSecurityMutationValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(AdministrativeSecurityAudit.MaxReasonLength);
    }
}
