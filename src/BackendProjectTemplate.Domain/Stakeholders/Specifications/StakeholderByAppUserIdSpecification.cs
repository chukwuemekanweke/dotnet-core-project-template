using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class StakeholderByAppUserIdSpecification : Specification<Stakeholder>
{
    public StakeholderByAppUserIdSpecification(Guid appUserId)
    {
        Where(stakeholder => stakeholder.AppUserId == appUserId);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
