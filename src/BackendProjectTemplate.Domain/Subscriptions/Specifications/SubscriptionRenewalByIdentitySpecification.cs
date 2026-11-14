using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionRenewalByIdentitySpecification : Specification<SubscriptionRenewal>
{
    public SubscriptionRenewalByIdentitySpecification(Guid subscriptionId, DateTimeOffset targetPeriodStartUtc)
    {
        Where(renewal => renewal.SubscriptionId == subscriptionId && renewal.TargetPeriodStartUtc == targetPeriodStartUtc);
        AddInclude(renewal => renewal.Attempts);
        EnableTracking();
    }
}
