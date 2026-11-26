namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

/// <summary>
/// Administrative audit list filters. <c>tenantId</c> matches the affected (target) tenant, <c>fromUtc</c> is
/// inclusive and <c>toUtc</c> exclusive. Results are ordered by <c>occurredAtUtc</c> then <c>id</c>, newest first.
/// </summary>
public sealed record ListAdministrativeAuditLogsRequest(
    string? Search = null,
    string? Action = null,
    Guid? ActorUserId = null,
    string? TargetType = null,
    Guid? TargetId = null,
    Guid? TenantId = null,
    string? CorrelationId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Page = 1,
    int PageSize = 20) : IAdministrativeAuditFilterRequest;
