namespace BackendProjectTemplate.Domain.Common.Auditing;

public interface ICurrentActorAccessor : ICurrentActor
{
    void Set(string actorId, Guid? tenantId, string correlationId);
}
