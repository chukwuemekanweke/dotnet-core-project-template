using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    Guid PlanId,
    string Name,
    decimal Price,
    ActorContext ActorContext);
