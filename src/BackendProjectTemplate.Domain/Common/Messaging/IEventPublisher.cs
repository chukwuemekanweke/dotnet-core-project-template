using BackendProjectTemplate.Contracts.Events;

namespace BackendProjectTemplate.Domain.Common.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent;
}
