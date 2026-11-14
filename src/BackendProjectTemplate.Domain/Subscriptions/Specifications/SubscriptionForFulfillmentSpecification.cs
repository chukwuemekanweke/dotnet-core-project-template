using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionForFulfillmentSpecification : Specification<Subscription>
{
    public SubscriptionForFulfillmentSpecification(Guid subscriptionId)
    {
        Where(subscription => subscription.Id == subscriptionId);
        AddInclude(subscription => subscription.Periods);
        AddInclude(subscription => subscription.StatusHistory);
        EnableTracking();
    }
}
