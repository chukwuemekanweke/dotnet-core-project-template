using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class BillingAccountByOwnerSpecification : Specification<BillingAccount>
{
    public BillingAccountByOwnerSpecification(
        Guid tenantId,
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId)
    {
        Where(account =>
            account.TenantId == tenantId
            && account.OwnerType == ownerType
            && account.StakeholderId == stakeholderId);
    }
}
