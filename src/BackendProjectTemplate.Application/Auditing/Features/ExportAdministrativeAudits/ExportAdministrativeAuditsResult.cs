namespace BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;

public sealed record ExportAdministrativeAuditsResult(
    ExportAdministrativeAuditsStatus Status,
    string? Csv,
    int RowCount,
    string? Error);
