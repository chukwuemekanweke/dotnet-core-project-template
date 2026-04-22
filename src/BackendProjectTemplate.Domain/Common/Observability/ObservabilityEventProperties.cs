using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Common.Observability;

public static class ObservabilityEventProperties
{
    public static Dictionary<string, string> Create(
        ICurrentActor currentActor,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.CorrelationIdPropertyName] = string.IsNullOrWhiteSpace(currentActor.CorrelationId)
                ? Guid.CreateVersion7().ToString("N")
                : currentActor.CorrelationId,
            [Observability.FlowIdPropertyName] = currentActor.FlowId ?? string.Empty
        };

        if (currentActor.TenantId.HasValue)
        {
            properties[Observability.TenantIdPropertyName] = currentActor.TenantId.Value.ToString();
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
}
