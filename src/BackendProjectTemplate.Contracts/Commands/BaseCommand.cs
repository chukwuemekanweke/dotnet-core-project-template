namespace BackendProjectTemplate.Contracts.Commands;

public abstract record BaseCommand
{
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; init; } = Guid.NewGuid();
}
