using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;

public sealed record CancelSubscriptionImmediatelyCommand(
    Guid SubscriptionId,
    Guid TenantId,
    string Reason,
    ActorContext ActorContext);
