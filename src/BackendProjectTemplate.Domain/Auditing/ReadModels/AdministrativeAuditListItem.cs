using System.Text.Json;

namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

/// <summary>
/// One administrative audit record. <see cref="Changes"/> is structured JSON
/// (<c>{"field": {"previous": ..., "current": ...}}</c>) rather than an escaped string.
/// </summary>
public sealed record AdministrativeAuditListItem(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    AdministrativeAuditActorSummary Actor,
    string Action,
    AdministrativeAuditTargetSummary Target,
    string? Reason,
    Guid? ActorTenantId,
    Guid? TargetTenantId,
    string CorrelationId,
    string? FlowId,
    JsonElement? Changes);
