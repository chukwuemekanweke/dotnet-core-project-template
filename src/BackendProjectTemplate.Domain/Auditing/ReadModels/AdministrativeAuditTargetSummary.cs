namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

public sealed record AdministrativeAuditTargetSummary(
    string Type,
    Guid Id,
    string? DisplayName);
