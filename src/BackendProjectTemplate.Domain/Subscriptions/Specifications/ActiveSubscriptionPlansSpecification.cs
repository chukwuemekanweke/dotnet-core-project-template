using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class ActiveSubscriptionPlansSpecification : Specification<SubscriptionPlan>
{
    public ActiveSubscriptionPlansSpecification()
    {
        Where(plan => plan.IsActive);
        ApplyOrderBy(plan => plan.Price);
        ApplyThenBy(plan => plan.Code);
    }
}
