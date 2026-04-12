using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class TenantEmailBaseTemplateByTenantIdSpecification : Specification<TenantEmailBaseTemplate>
{
    public TenantEmailBaseTemplateByTenantIdSpecification(Guid tenantId)
    {
        Where(template => template.TenantId == tenantId);
    }
}
