using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    Guid PlanId,
    string Name,
    IReadOnlyDictionary<BillingInterval, decimal> Prices,
    string? Description,
    IReadOnlyList<string> Features,
    ActorContext ActorContext,
    string? Reason = null);
