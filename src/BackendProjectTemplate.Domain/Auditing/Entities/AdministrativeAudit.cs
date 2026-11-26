using BackendProjectTemplate.Domain.Common.Entities;
using System.Text.Json;

namespace BackendProjectTemplate.Domain.Auditing.Entities;

/// <summary>
/// An append-only record of one successful privileged administrative mutation performed by an authenticated
/// human administrator. Actor, target and tenant identifiers are historical logical references (no foreign
/// keys), and display names are snapshots taken when the action was recorded, so records stay readable after
/// the referenced resources are renamed or deleted. Records expose no mutation methods once created.
/// </summary>
public sealed class AdministrativeAudit : Entity, IAggregateRoot
{
    public const int MaxDisplayNameLength = 250;
    public const int MaxActionLength = 128;
    public const int MaxReasonLength = 1000;
    public const int MaxTargetTypeLength = 100;
    public const int MaxCorrelationLength = 128;
    public const int MaxChangesLength = 16_384;

    private AdministrativeAudit()
    {
    }

    public Guid ActorUserId { get; private set; }
    public Guid? ActorStakeholderId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string TargetType { get; private set; } = string.Empty;
    public Guid TargetId { get; private set; }
    public string? TargetDisplayName { get; private set; }
    public Guid? ActorTenantId { get; private set; }
    public Guid? TargetTenantId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? FlowId { get; private set; }
    public string? Changes { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AdministrativeAudit Create(
        AdministrativeAuditActor actor,
        string action,
        string targetType,
        Guid targetId,
        string? targetDisplayName,
        Guid? targetTenantId,
        string? reason,
        string? changes,
        Guid? actorTenantId,
        string correlationId,
        string? flowId,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.UserId == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator user is required.", nameof(actor));
        }

        if (actor.StakeholderId == Guid.Empty)
        {
            throw new ArgumentException("Actor stakeholder must be null or a non-empty identifier.", nameof(actor));
        }

        if (!AdministrativeAuditActions.IsRecordable(action))
        {
            throw new ArgumentException($"'{action}' is not a supported administrative audit action.", nameof(action));
        }

        if (!AdministrativeAuditTargetTypes.IsKnown(targetType))
        {
            throw new ArgumentException($"'{targetType}' is not a supported administrative audit target type.", nameof(targetType));
        }

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("A target identifier is required.", nameof(targetId));
        }

        ValidateTargetTenant(targetType, targetTenantId);
        if (actorTenantId == Guid.Empty)
        {
            throw new ArgumentException("Actor tenant must be null or a non-empty identifier.", nameof(actorTenantId));
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The occurrence time must be expressed in UTC.", nameof(occurredAtUtc));
        }

        return new AdministrativeAudit
        {
            ActorUserId = actor.UserId,
            ActorStakeholderId = actor.StakeholderId,
            ActorDisplayName = NormalizeRequired(actor.DisplayName, MaxDisplayNameLength, nameof(actor)),
            Action = action,
            Reason = NormalizeOptional(reason, MaxReasonLength, nameof(reason)),
            TargetType = targetType,
            TargetId = targetId,
            TargetDisplayName = NormalizeOptional(targetDisplayName, MaxDisplayNameLength, nameof(targetDisplayName)),
            ActorTenantId = actorTenantId,
            TargetTenantId = targetTenantId,
            CorrelationId = NormalizeRequired(correlationId, MaxCorrelationLength, nameof(correlationId)),
            FlowId = TruncateOptional(flowId, MaxCorrelationLength),
            Changes = ValidateChanges(changes),
            OccurredAtUtc = occurredAtUtc
        };
    }

    private static void ValidateTargetTenant(string targetType, Guid? targetTenantId)
    {
        if (targetTenantId == Guid.Empty)
        {
            throw new ArgumentException("Target tenant must be null or a non-empty identifier.", nameof(targetTenantId));
        }

        var tenantScoped = AdministrativeAuditTargetTypes.IsTenantScoped(targetType);
        if (tenantScoped && !targetTenantId.HasValue)
        {
            throw new ArgumentException(
                $"Target type '{targetType}' is tenant-scoped; the affected tenant must be resolved from the target resource.",
                nameof(targetTenantId));
        }

        if (!tenantScoped && targetTenantId.HasValue)
        {
            throw new ArgumentException($"Target type '{targetType}' is global and cannot carry a target tenant.", nameof(targetTenantId));
        }
    }

    private static string? ValidateChanges(string? changes)
    {
        if (changes is null)
        {
            return null;
        }

        if (changes.Length > MaxChangesLength)
        {
            throw new ArgumentException($"Changes must not exceed {MaxChangesLength} characters.", nameof(changes));
        }

        try
        {
            using var document = JsonDocument.Parse(changes);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.EnumerateObject().Any())
            {
                throw new ArgumentException("Changes must be a non-empty JSON object.", nameof(changes));
            }

            foreach (var field in root.EnumerateObject())
            {
                if (!AdministrativeAuditChanges.IsValidFieldName(field.Name) ||
                    field.Value.ValueKind != JsonValueKind.Object ||
                    field.Value.EnumerateObject().Count() != 2 ||
                    !field.Value.TryGetProperty(AdministrativeAuditChanges.PreviousProperty, out _) ||
                    !field.Value.TryGetProperty(AdministrativeAuditChanges.CurrentProperty, out _))
                {
                    throw new ArgumentException(
                        $"Change '{field.Name}' must be a camelCase field holding exactly 'previous' and 'current'.",
                        nameof(changes));
                }
            }

            return root.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Changes must be valid JSON.", nameof(changes), exception);
        }
    }

    private static string NormalizeRequired(string? value, int maxLength, string parameterName) =>
        NormalizeOptional(value, maxLength, parameterName)
        ?? throw new ArgumentException("A value is required.", parameterName);

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentException($"Value must not exceed {maxLength} characters.", parameterName);
    }

    /// <summary>
    /// The flow identifier is a client-supplied tracing hint, so an oversized value is truncated rather than
    /// failing the administrative mutation it describes.
    /// </summary>
    private static string? TruncateOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
