using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionPurchaseByOperationIdSpecification : Specification<SubscriptionPurchase>
{
    public SubscriptionPurchaseByOperationIdSpecification(Guid tenantId, Guid stakeholderId, Guid operationId) =>
        Where(purchase =>
            purchase.TenantId == tenantId &&
            purchase.InitiatedByStakeholderId == stakeholderId &&
            purchase.OperationId == operationId);
}
