using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// What an administrative vertical slice asks <see cref="IAdministrativeAuditRecorder"/> to stage. The actor is
/// always resolved from the server-side <see cref="ActorContext"/>; <paramref name="ActorUserId"/> is only the
/// Identity user already established from the validated access token, and must agree with that context.
/// </summary>
public sealed record AdministrativeAuditEntry(
    ActorContext ActorContext,
    string Action,
    string TargetType,
    Guid TargetId,
    string? TargetDisplayName,
    Guid? TargetTenantId,
    string? Reason,
    AdministrativeAuditChanges? Changes,
    Guid? ActorUserId = null);
