using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionPurchaseBySubscriptionIdSpecification : Specification<SubscriptionPurchase>
{
    public SubscriptionPurchaseBySubscriptionIdSpecification(Guid subscriptionId)
    {
        Where(purchase => purchase.SubscriptionId == subscriptionId);
        EnableTracking();
    }
}
