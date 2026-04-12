using System.Diagnostics;
using BackendProjectTemplate.Contracts.Common;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        CustomTelemetryContext.SetProperty(Observability.MessageTypePropertyName, typeof(TMessage).Name);

        if (message is BaseEvent baseEvent)
        {
            currentActorAccessor.Set(
                baseEvent.ActorId,
                baseEvent.TenantId == Guid.Empty ? null : baseEvent.TenantId,
                baseEvent.CorrelationId);

            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseEvent.MessageId.ToString())
                .SetProperty("OccurredAt", baseEvent.OccuredAt.ToString("O"))
                .SetProperty("ActorId", baseEvent.ActorId)
                .SetProperty("TenantId", baseEvent.TenantId == Guid.Empty ? string.Empty : baseEvent.TenantId.ToString())
                .SetProperty("CorrelationId", baseEvent.CorrelationId);
        }
        else if (message is BaseCommand baseCommand)
        {
            currentActorAccessor.Set(
                baseCommand.ActorId,
                baseCommand.TenantId == Guid.Empty ? null : baseCommand.TenantId,
                baseCommand.CorrelationId);

            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseCommand.MessageId.ToString())
                .SetProperty("RequestedAt", baseCommand.RequestedAt.ToString("O"))
                .SetProperty("ActorId", baseCommand.ActorId)
                .SetProperty("TenantId", baseCommand.TenantId == Guid.Empty ? string.Empty : baseCommand.TenantId.ToString())
                .SetProperty("CorrelationId", baseCommand.CorrelationId);
        }
        else
        {
            currentActorAccessor.Set(
                ActorDefaults.SystemActorId,
                null,
                Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N"));
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
