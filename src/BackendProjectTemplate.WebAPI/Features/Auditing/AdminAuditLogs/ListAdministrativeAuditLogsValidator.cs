using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

public sealed class ListAdministrativeAuditLogsValidator : AdministrativeAuditFilterValidator<ListAdministrativeAuditLogsRequest>
{
    public const int MaxPageSize = 100;

    public ListAdministrativeAuditLogsValidator()
    {
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
