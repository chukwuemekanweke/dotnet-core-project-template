using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionAllocationBySubscriptionIdSpecification : Specification<SubscriptionAllocation>
{
    public SubscriptionAllocationBySubscriptionIdSpecification(Guid subscriptionId) =>
        Where(allocation => allocation.SubscriptionId == subscriptionId);
}
