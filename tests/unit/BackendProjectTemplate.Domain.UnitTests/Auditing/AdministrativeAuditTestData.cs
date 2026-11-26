using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Auditing;

internal static class AdministrativeAuditTestData
{
    public static readonly DateTimeOffset OccurredAtUtc = new(2026, 11, 24, 16, 2, 0, TimeSpan.Zero);

    public static AdministrativeAuditActor Actor { get; } = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "Chidi Nwosu");

    public static AdministrativeAudit Create(
        AdministrativeAuditActor? actor = null,
        string action = AdministrativeAuditActions.Users.Locked,
        string targetType = AdministrativeAuditTargetTypes.User,
        Guid? targetId = null,
        string? targetDisplayName = "Tunde Bakare",
        Guid? targetTenantId = null,
        string? reason = "Suspected account takeover",
        string? changes = null,
        Guid? actorTenantId = null,
        string correlationId = "request-correlation-id",
        string? flowId = "flow-id",
        DateTimeOffset? occurredAtUtc = null) =>
        AdministrativeAudit.Create(
            actor ?? Actor,
            action,
            targetType,
            targetId ?? Guid.CreateVersion7(),
            targetDisplayName,
            targetTenantId,
            reason,
            changes,
            actorTenantId,
            correlationId,
            flowId,
            occurredAtUtc ?? OccurredAtUtc);
}
