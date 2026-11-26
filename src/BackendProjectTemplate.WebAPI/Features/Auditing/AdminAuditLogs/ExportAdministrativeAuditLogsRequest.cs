namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

/// <summary>Administrative audit CSV export filters; identical in meaning to the list filters.</summary>
public sealed record ExportAdministrativeAuditLogsRequest(
    string? Search = null,
    string? Action = null,
    Guid? ActorUserId = null,
    string? TargetType = null,
    Guid? TargetId = null,
    Guid? TenantId = null,
    string? CorrelationId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null) : IAdministrativeAuditFilterRequest;
