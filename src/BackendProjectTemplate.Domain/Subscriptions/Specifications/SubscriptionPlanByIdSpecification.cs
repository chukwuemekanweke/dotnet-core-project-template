using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionPlanByIdSpecification : Specification<SubscriptionPlan>
{
    public SubscriptionPlanByIdSpecification(Guid planId) => Where(plan => plan.Id == planId);
}
