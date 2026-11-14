using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class ActiveSubscriptionByBillingAccountSpecification : Specification<Subscription>
{
    public ActiveSubscriptionByBillingAccountSpecification(Guid tenantId, Guid billingAccountId)
    {
        Where(subscription =>
            subscription.TenantId == tenantId &&
            subscription.BillingAccountId == billingAccountId &&
            subscription.Status == SubscriptionStatus.Active);
        ApplyOrderByDescending(subscription => subscription.CreatedAtUtc);
        AddInclude(subscription => subscription.StatusHistory);
        EnableTracking();
    }
}
