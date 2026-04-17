using System.Text.Json;
using System.Diagnostics;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Infrastructure.Messaging;

internal sealed class OutboxWriter(
    IRepository<OutboxMessage> repository,
    ICurrentActor currentActor) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task AddEventAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent =>
        AddEventMessageAsync(message, cancellationToken);

    public Task AddCommandAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand =>
        AddCommandMessageAsync(message, cancellationToken);

    private Task AddEventMessageAsync<TEvent>(
        TEvent message,
        CancellationToken cancellationToken)
        where TEvent : BaseEvent
    {
        var messageType = message.GetType();
        var typeName = messageType.FullName ?? messageType.Name;
        var payload = JsonSerializer.Serialize(message, messageType, SerializerOptions);
        var outboxMessage = OutboxMessage.CreateEvent(
            message.MessageId,
            typeName,
            payload,
            message.OccuredAt,
            currentActor.CorrelationId,
            Activity.Current?.Id);

        return repository.AddAsync(outboxMessage, cancellationToken);
    }

    private Task AddCommandMessageAsync<TCommand>(
        TCommand message,
        CancellationToken cancellationToken)
        where TCommand : BaseCommand
    {
        var messageType = message.GetType();
        var typeName = messageType.FullName ?? messageType.Name;
        var payload = JsonSerializer.Serialize(message, messageType, SerializerOptions);
        var outboxMessage = OutboxMessage.CreateCommand(
            message.MessageId,
            typeName,
            payload,
            message.RequestedAt,
            currentActor.CorrelationId,
            Activity.Current?.Id);

        return repository.AddAsync(outboxMessage, cancellationToken);
    }
}
