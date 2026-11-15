using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionPlanByCodeSpecification : Specification<SubscriptionPlan>
{
    public SubscriptionPlanByCodeSpecification(string normalizedCode) =>
        Where(plan => plan.Code == normalizedCode);
}
