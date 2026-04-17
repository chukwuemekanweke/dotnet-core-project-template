using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class AppUserStakeholderByAppUserIdSpecification : Specification<AppUserStakeholder>
{
    public AppUserStakeholderByAppUserIdSpecification(Guid appUserId)
    {
        Where(appUserStakeholder => appUserStakeholder.AppUserId == appUserId);
        AddInclude(appUserStakeholder => appUserStakeholder.AppUser);
        AddInclude(appUserStakeholder => appUserStakeholder.Stakeholder);
        AddInclude(appUserStakeholder => appUserStakeholder.Stakeholder.StakeholderType);
    }
}
