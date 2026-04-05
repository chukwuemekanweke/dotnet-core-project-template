using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;

namespace BackendProjectTemplate.Infrastructure.Messaging;

internal sealed class EventPublisher(IOutboxWriter outboxWriter) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent =>
        outboxWriter.AddEventAsync(message, cancellationToken);
}
