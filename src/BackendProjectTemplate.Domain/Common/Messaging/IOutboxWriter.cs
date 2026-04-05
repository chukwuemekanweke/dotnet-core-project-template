using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;

namespace BackendProjectTemplate.Domain.Common.Messaging;

public interface IOutboxWriter
{
    Task AddEventAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent;

    Task AddCommandAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;
}
