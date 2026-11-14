using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionRenewalByIdSpecification : Specification<SubscriptionRenewal>
{
    public SubscriptionRenewalByIdSpecification(Guid renewalId)
    {
        Where(renewal => renewal.Id == renewalId);
        AddInclude(renewal => renewal.Attempts);
        EnableTracking();
    }
}
