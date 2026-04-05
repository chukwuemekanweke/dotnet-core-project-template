namespace BackendProjectTemplate.Contracts.Events;

public abstract record BaseEvent
{
    public DateTimeOffset OccuredAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; init; } = Guid.NewGuid();
}
