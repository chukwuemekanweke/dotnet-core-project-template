using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class UnsettledSubscriptionRenewalByPeriodSpecification : Specification<SubscriptionRenewal>
{
    public UnsettledSubscriptionRenewalByPeriodSpecification(Guid subscriptionId, DateTimeOffset periodStartUtc) =>
        Where(renewal =>
            renewal.SubscriptionId == subscriptionId &&
            renewal.TargetPeriodStartUtc == periodStartUtc &&
            renewal.Status != SubscriptionRenewalStatus.Fulfilled &&
            renewal.Status != SubscriptionRenewalStatus.Cancelled &&
            renewal.Status != SubscriptionRenewalStatus.FulfillmentBlocked);
}
