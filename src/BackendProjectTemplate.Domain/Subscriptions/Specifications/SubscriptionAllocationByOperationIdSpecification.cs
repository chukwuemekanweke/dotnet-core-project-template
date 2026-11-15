using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionAllocationByOperationIdSpecification : Specification<SubscriptionAllocation>
{
    public SubscriptionAllocationByOperationIdSpecification(Guid operationId) =>
        Where(allocation => allocation.OperationId == operationId);
}
