namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

public interface IAdministrativeAuditReadModelRepository
{
    Task<AdministrativeAuditListPage> ListAsync(
        AdministrativeAuditListRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns at most <paramref name="maxRows"/> rows in the list order, so callers can detect an oversized
    /// export by requesting one more row than they allow.
    /// </summary>
    Task<IReadOnlyList<AdministrativeAuditExportRow>> ListForExportAsync(
        AdministrativeAuditFilter filter,
        int maxRows,
        CancellationToken cancellationToken);
}
