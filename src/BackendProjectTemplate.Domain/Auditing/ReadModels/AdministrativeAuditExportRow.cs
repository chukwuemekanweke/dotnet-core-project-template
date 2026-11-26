namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

/// <summary>The allowlisted fields written to an administrative audit CSV export.</summary>
public sealed record AdministrativeAuditExportRow(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid ActorUserId,
    string ActorDisplayName,
    string Action,
    string TargetType,
    Guid TargetId,
    string? TargetDisplayName,
    string? Reason,
    Guid? TargetTenantId,
    string CorrelationId);
