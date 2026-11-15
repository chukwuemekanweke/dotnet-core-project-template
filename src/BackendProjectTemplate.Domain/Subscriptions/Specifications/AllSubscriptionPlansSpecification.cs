using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class AllSubscriptionPlansSpecification : Specification<SubscriptionPlan>
{
    public AllSubscriptionPlansSpecification(bool? isActive)
    {
        Where(plan => isActive == null || plan.IsActive == isActive);
        ApplyOrderBy(plan => plan.Code);
    }
}
