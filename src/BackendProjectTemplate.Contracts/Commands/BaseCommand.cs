using Chidelu.Integration.Messaging.RabbitMQ.Core;

namespace BackendProjectTemplate.Contracts.Commands;

public abstract record BaseCommand : ICommand
{
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; init; } = Guid.CreateVersion7();
}
