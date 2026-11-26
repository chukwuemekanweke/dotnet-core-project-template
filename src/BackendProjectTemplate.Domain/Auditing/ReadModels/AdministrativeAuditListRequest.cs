namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

public sealed record AdministrativeAuditListRequest(
    AdministrativeAuditFilter Filter,
    int Page,
    int PageSize);
