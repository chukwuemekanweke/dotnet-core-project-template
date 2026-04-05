using Chidelu.Integration.Messaging.RabbitMQ.Core;

namespace BackendProjectTemplate.Contracts.Events;

public abstract record BaseEvent : IEvent
{
    public DateTimeOffset OccuredAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; init; } = Guid.CreateVersion7();
}
