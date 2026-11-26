namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

public sealed record AdministrativeAuditListPage(
    IReadOnlyList<AdministrativeAuditListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
