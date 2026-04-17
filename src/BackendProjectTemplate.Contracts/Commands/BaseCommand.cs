using Chidelu.Integration.Messaging.RabbitMQ.Core;

namespace BackendProjectTemplate.Contracts.Commands;

public abstract record BaseCommand : ICommand
{
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; set; } = Guid.CreateVersion7();
    public Guid? StakeholderId { get; set; }
    public Guid TenantId { get; set; }
}
