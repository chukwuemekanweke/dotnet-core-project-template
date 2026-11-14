using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments;

public interface IBillingAccountResolver
{
    Task<BillingAccount> ResolveOrCreateAsync(
        Guid tenantId,
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId,
        CancellationToken cancellationToken);
}
