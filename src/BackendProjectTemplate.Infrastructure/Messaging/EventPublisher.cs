using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;

namespace BackendProjectTemplate.Infrastructure.Messaging;

internal sealed class EventPublisher(
    IOutboxWriter outboxWriter,
    ICurrentActor currentActor) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent
    {
        message.ActorId = currentActor.ActorId;
        if (message.TenantId == Guid.Empty && currentActor.TenantId.HasValue)
        {
            message.TenantId = currentActor.TenantId.Value;
        }

        message.CorrelationId = currentActor.CorrelationId;

        return outboxWriter.AddEventAsync(message, cancellationToken);
    }
}
