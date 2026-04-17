using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class AppUserStakeholderByStakeholderIdSpecification : Specification<AppUserStakeholder>
{
    public AppUserStakeholderByStakeholderIdSpecification(Guid stakeholderId)
    {
        Where(appUserStakeholder => appUserStakeholder.StakeholderId == stakeholderId);
        AddInclude(appUserStakeholder => appUserStakeholder.Stakeholder);
        AddInclude(appUserStakeholder => appUserStakeholder.Stakeholder.StakeholderType);
    }
}
