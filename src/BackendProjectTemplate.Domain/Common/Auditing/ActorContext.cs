namespace BackendProjectTemplate.Domain.Common.Auditing;

public sealed record ActorContext(Guid? StakeholderId, Guid? TenantId, string CorrelationId, string FlowId)
{
    public static ActorContext FromCurrentActor(ICurrentActor currentActor) => new(
        Guid.TryParse(currentActor.ActorId, out var stakeholderId) ? stakeholderId : null,
        currentActor.TenantId,
        currentActor.CorrelationId,
        currentActor.FlowId);
}
