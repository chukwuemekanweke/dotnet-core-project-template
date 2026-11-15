using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;

public sealed record SetSubscriptionPlanActivationCommand(
    Guid PlanId,
    bool IsActive,
    ActorContext ActorContext);
