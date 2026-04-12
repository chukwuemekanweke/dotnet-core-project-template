using Chidelu.Integration.Messaging.RabbitMQ.Core;
using BackendProjectTemplate.Contracts.Common;

namespace BackendProjectTemplate.Contracts.Events;

public abstract record BaseEvent : IEvent
{
    public DateTimeOffset OccuredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; set; } = Guid.CreateVersion7();
    public string ActorId { get; set; } = ActorDefaults.SystemActorId;
    public Guid TenantId { get; set; }
    public string CorrelationId { get; set; } = Guid.CreateVersion7().ToString("N");
}
