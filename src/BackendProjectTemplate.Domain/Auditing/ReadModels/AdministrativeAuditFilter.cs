namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

/// <summary>
/// Filters shared by the administrative audit list and export. <see cref="TenantId"/> matches the affected
/// (target) tenant only and narrows results; it never grants access. <see cref="FromUtc"/> is inclusive and
/// <see cref="ToUtc"/> exclusive.
/// </summary>
public sealed record AdministrativeAuditFilter(
    string? Search,
    string? Action,
    Guid? ActorUserId,
    string? TargetType,
    Guid? TargetId,
    Guid? TenantId,
    string? CorrelationId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);
