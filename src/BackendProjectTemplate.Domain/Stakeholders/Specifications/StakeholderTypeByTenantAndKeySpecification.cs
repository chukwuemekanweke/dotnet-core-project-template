using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class StakeholderTypeByTenantAndKeySpecification : Specification<StakeholderType>
{
    public StakeholderTypeByTenantAndKeySpecification(Guid tenantId, string key)
    {
        var normalizedKey = key.Trim();
        Where(stakeholderType =>
            stakeholderType.TenantId == tenantId &&
            stakeholderType.Key == normalizedKey);
    }
}
