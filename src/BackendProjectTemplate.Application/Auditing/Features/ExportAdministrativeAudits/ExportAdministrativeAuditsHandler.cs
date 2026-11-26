using BackendProjectTemplate.Domain.Auditing.ReadModels;

namespace BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;

/// <summary>
/// Exports the filtered administrative audit journal as CSV using the same filter semantics and ordering as the
/// list. Exports are bounded: when the filter matches more than <see cref="MaxRows"/> records nothing is
/// exported and the caller is told to narrow the filter, so an export is never silently truncated.
/// </summary>
public sealed class ExportAdministrativeAuditsHandler(IAdministrativeAuditReadModelRepository repository)
{
    public const int MaxRows = 10_000;

    public async Task<ExportAdministrativeAuditsResult> HandleAsync(
        ExportAdministrativeAuditsQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await repository.ListForExportAsync(query.Filter, MaxRows + 1, cancellationToken);
        if (rows.Count > MaxRows)
        {
            return new ExportAdministrativeAuditsResult(
                ExportAdministrativeAuditsStatus.TooManyRecords,
                null,
                0,
                $"The filter matches more than {MaxRows} audit records. Narrow the filter (for example the date range) and export again.");
        }

        return new ExportAdministrativeAuditsResult(
            ExportAdministrativeAuditsStatus.Exported,
            AdministrativeAuditCsvWriter.Write(rows),
            rows.Count,
            null);
    }
}
