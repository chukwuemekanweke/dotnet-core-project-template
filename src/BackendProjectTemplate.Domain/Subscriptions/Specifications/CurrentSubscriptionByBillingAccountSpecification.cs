using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class CurrentSubscriptionByBillingAccountSpecification : Specification<Subscription>
{
    public CurrentSubscriptionByBillingAccountSpecification(Guid tenantId, Guid billingAccountId, bool enableTracking = false)
    {
        Where(subscription =>
            subscription.TenantId == tenantId &&
            subscription.BillingAccountId == billingAccountId &&
            (subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.Pending));
        ApplyOrderByDescending(subscription => subscription.Status == SubscriptionStatus.Active);
        ApplyThenByDescending(subscription => subscription.CreatedAtUtc);
        if (enableTracking)
        {
            AddInclude(subscription => subscription.StatusHistory);
            EnableTracking();
        }
    }
}
