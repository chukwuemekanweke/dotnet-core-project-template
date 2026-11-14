using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class StakeholderByTenantAndIdSpecification : Specification<Stakeholder>
{
    public StakeholderByTenantAndIdSpecification(Guid tenantId, Guid stakeholderId)
    {
        Where(stakeholder => stakeholder.TenantId == tenantId && stakeholder.Id == stakeholderId);
    }
}
