using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class OpenSubscriptionRenewalsBySubscriptionSpecification : Specification<SubscriptionRenewal>
{
    public OpenSubscriptionRenewalsBySubscriptionSpecification(Guid subscriptionId)
    {
        Where(renewal =>
            renewal.SubscriptionId == subscriptionId &&
            (renewal.Status == SubscriptionRenewalStatus.Pending ||
             renewal.Status == SubscriptionRenewalStatus.AwaitingAuthorization ||
             renewal.Status == SubscriptionRenewalStatus.RetryScheduled));
        EnableTracking();
    }
}
