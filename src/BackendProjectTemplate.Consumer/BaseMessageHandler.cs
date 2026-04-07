using System.Diagnostics;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        CustomTelemetryContext.SetProperty(Observability.MessageTypePropertyName, typeof(TMessage).Name);

        if (message is BaseEvent baseEvent)
        {
            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseEvent.MessageId.ToString())
                .SetProperty("OccurredAt", baseEvent.OccuredAt.ToString("O"));
        }
        else if (message is BaseCommand baseCommand)
        {
            CustomTelemetryContext
                .SetProperty(Observability.MessageIdPropertyName, baseCommand.MessageId.ToString())
                .SetProperty("RequestedAt", baseCommand.RequestedAt.ToString("O"));
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
