using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Common.Observability;

public static class ObservabilityEventProperties
{
    public static Dictionary<string, string> Create(
        ActorContext actorContext,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.CorrelationIdPropertyName] = string.IsNullOrWhiteSpace(actorContext.CorrelationId)
                ? Guid.CreateVersion7().ToString("N")
                : actorContext.CorrelationId,
            [Observability.FlowIdPropertyName] = actorContext.FlowId ?? string.Empty
        };

        if (actorContext.TenantId.HasValue)
        {
            properties[Observability.TenantIdPropertyName] = actorContext.TenantId.Value.ToString();
        }

        if (stakeholderId.HasValue)
        {
            properties[Observability.StakeholderIdPropertyName] = stakeholderId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            properties[Observability.FailureReasonPropertyName] = failureReason;
        }

        if (additionalProperties is null)
        {
            return properties;
        }

        foreach (var property in additionalProperties)
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    public static Dictionary<string, string> Create(
        ICurrentActor currentActor,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var actorContext = ActorContext.FromCurrentActor(currentActor);
        return Create(actorContext, stakeholderId, failureReason, additionalProperties);
    }
}
