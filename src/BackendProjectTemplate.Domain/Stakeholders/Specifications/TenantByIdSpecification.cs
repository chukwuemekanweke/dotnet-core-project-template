using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class TenantByIdSpecification : Specification<Tenant>
{
    public TenantByIdSpecification(Guid tenantId)
    {
        Where(tenant => tenant.Id == tenantId);
    }
}
