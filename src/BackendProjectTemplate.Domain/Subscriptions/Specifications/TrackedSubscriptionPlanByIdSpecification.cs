using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class TrackedSubscriptionPlanByIdSpecification : Specification<SubscriptionPlan>
{
    public TrackedSubscriptionPlanByIdSpecification(Guid planId)
    {
        Where(plan => plan.Id == planId);
        EnableTracking();
    }
}
