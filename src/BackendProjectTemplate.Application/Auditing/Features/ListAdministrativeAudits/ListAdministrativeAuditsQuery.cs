using BackendProjectTemplate.Domain.Auditing.ReadModels;

namespace BackendProjectTemplate.Application.Auditing.Features.ListAdministrativeAudits;

public sealed record ListAdministrativeAuditsQuery(
    AdministrativeAuditFilter Filter,
    int Page,
    int PageSize);
