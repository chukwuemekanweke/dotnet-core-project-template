using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class ActiveSubscriptionPlanByIdSpecification : Specification<SubscriptionPlan>
{
    public ActiveSubscriptionPlanByIdSpecification(Guid planId) =>
        Where(plan => plan.Id == planId && plan.IsActive);
}
