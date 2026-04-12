using BackendProjectTemplate.Contracts.Common;
using BackendProjectTemplate.Domain.Common.Auditing;
using System.Diagnostics;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed class CurrentActorAccessor : ICurrentActorAccessor
{
    public string ActorId { get; private set; } = ActorDefaults.SystemActorId;
    public Guid? TenantId { get; private set; }
    public string CorrelationId { get; private set; } = Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N");

    public void Set(string actorId, Guid? tenantId, string correlationId)
    {
        ActorId = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
        TenantId = tenantId;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.CreateVersion7().ToString("N") : correlationId;
    }
}
