using BackendProjectTemplate.Domain.Auditing.ReadModels;

namespace BackendProjectTemplate.Application.Auditing.Features.ListAdministrativeAudits;

/// <summary>
/// Lists the platform-wide administrative audit journal. Access is granted by the <c>audit.read</c> permission
/// before this handler runs; the tenant filter only narrows results to records affecting that tenant.
/// </summary>
public sealed class ListAdministrativeAuditsHandler(IAdministrativeAuditReadModelRepository repository)
{
    public Task<AdministrativeAuditListPage> HandleAsync(
        ListAdministrativeAuditsQuery query,
        CancellationToken cancellationToken) =>
        repository.ListAsync(
            new AdministrativeAuditListRequest(query.Filter, query.Page, query.PageSize),
            cancellationToken);
}
