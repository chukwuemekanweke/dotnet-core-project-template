using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class AdministrativeSecurityAudit : Entity, IAggregateRoot
{
    public const int MaxReasonLength = 500;
    public const int MaxRolesLength = 500;
    public const int MaxCorrelationLength = 128;

    private AdministrativeSecurityAudit()
    {
    }

    private AdministrativeSecurityAudit(
        Guid actorUserId,
        Guid targetUserId,
        AdministrativeSecurityAction action,
        string reason,
        DateTimeOffset occurredAtUtc,
        Guid? tenantId,
        string correlationId,
        string flowId,
        string? previousRoles,
        string? resultingRoles)
    {
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        Action = action;
        Reason = NormalizeRequired(reason, MaxReasonLength, nameof(reason));
        OccurredAtUtc = occurredAtUtc;
        TenantId = tenantId;
        CorrelationId = NormalizeRequired(correlationId, MaxCorrelationLength, nameof(correlationId));
        FlowId = NormalizeOptional(flowId, MaxCorrelationLength);
        PreviousRoles = NormalizeOptional(previousRoles, MaxRolesLength);
        ResultingRoles = NormalizeOptional(resultingRoles, MaxRolesLength);
        Succeeded = true;
    }

    public Guid ActorUserId { get; private set; }
    public Guid TargetUserId { get; private set; }
    public AdministrativeSecurityAction Action { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? TenantId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? FlowId { get; private set; }
    public string? PreviousRoles { get; private set; }
    public string? ResultingRoles { get; private set; }
    public bool Succeeded { get; private set; }

    public static AdministrativeSecurityAudit CreateSucceeded(
        Guid actorUserId,
        Guid targetUserId,
        AdministrativeSecurityAction action,
        string reason,
        DateTimeOffset occurredAtUtc,
        Guid? tenantId,
        string correlationId,
        string flowId,
        IEnumerable<string>? previousRoles = null,
        IEnumerable<string>? resultingRoles = null) =>
        new(
            actorUserId,
            targetUserId,
            action,
            reason,
            occurredAtUtc,
            tenantId,
            correlationId,
            flowId,
            SerializeRoles(previousRoles),
            SerializeRoles(resultingRoles));

    private static string? SerializeRoles(IEnumerable<string>? roles) =>
        roles is null ? null : string.Join(',', roles.Order(StringComparer.Ordinal));

    private static string NormalizeRequired(string value, int maxLength, string parameterName) =>
        NormalizeOptional(value, maxLength)
        ?? throw new ArgumentException("A value is required.", parameterName);

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentException($"Value must not exceed {maxLength} characters.", nameof(value));
    }
}
