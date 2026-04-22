using System.Diagnostics;
using BackendProjectTemplate.Contracts.Common;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        CustomTelemetryContext.SetProperty(Observability.MessageTypePropertyName, typeof(TMessage).Name);

        if (message is BaseEvent baseEvent)
        {
            if (baseEvent.TenantId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid tenant id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseEvent.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseEvent.TenantId,
                messageContext.CorrelationId,
                baseEvent.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseEvent.MessageId.ToString())
                .SetProperty("OccurredAt", baseEvent.OccuredAt.ToString("O"))
                .SetProperty(Observability.StakeholderIdPropertyName, baseEvent.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.TenantIdPropertyName, baseEvent.TenantId.ToString())
                .SetProperty(Observability.CorrelationIdPropertyName, messageContext.CorrelationId)
                .SetProperty(Observability.FlowIdPropertyName, baseEvent.FlowId ?? string.Empty);
        }
        else if (message is BaseCommand baseCommand)
        {
            if (baseCommand.TenantId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid tenant id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseCommand.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseCommand.TenantId,
                messageContext.CorrelationId,
                baseCommand.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseCommand.MessageId.ToString())
                .SetProperty("RequestedAt", baseCommand.RequestedAt.ToString("O"))
                .SetProperty(Observability.StakeholderIdPropertyName, baseCommand.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.TenantIdPropertyName, baseCommand.TenantId.ToString())
                .SetProperty(Observability.CorrelationIdPropertyName, messageContext.CorrelationId)
                .SetProperty(Observability.FlowIdPropertyName, baseCommand.FlowId ?? string.Empty);
        }
        else
        {
            throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.");
        }

        foreach (var telemetryParameter in GetTelemetryParameters(message))
        {
            CustomTelemetryContext.SetProperty(telemetryParameter.Key, telemetryParameter.Value);
        }

        await HandleAsyncInternal(message, cancellationToken);
    }

    protected virtual IEnumerable<(string Key, string Value)> GetTelemetryParameters(TMessage message) => [];

    protected abstract Task HandleAsyncInternal(TMessage message, CancellationToken cancellationToken);
}
