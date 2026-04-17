using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class StakeholderReadModelRepository(IReadRepository<AppUserStakeholder> repository) : IStakeholderReadModelRepository
{
    public async Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default)
    {
        var appUserStakeholder = await repository.FirstOrDefaultAsync(
            new AppUserStakeholderByAppUserIdSpecification(appUserId),
            cancellationToken);

        return appUserStakeholder is null
            ? null
            : new StakeholderReadModel(
                appUserStakeholder.StakeholderId,
                appUserStakeholder.AppUserId,
                appUserStakeholder.AppUser.Email ?? string.Empty,
                appUserStakeholder.Stakeholder.TenantId,
                appUserStakeholder.Stakeholder.CountryId,
                appUserStakeholder.Stakeholder.StakeholderTypeId,
                appUserStakeholder.Stakeholder.FirstName,
                appUserStakeholder.Stakeholder.LastName,
                appUserStakeholder.Stakeholder.AvatarUrl,
                appUserStakeholder.Stakeholder.IsVerified);
    }

    public async Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default)
    {
        var appUserStakeholder = await repository.FirstOrDefaultAsync(
            new AppUserStakeholderByStakeholderIdSpecification(stakeholderId),
            cancellationToken);

        return appUserStakeholder is null
            ? null
            : new StakeholderReadModel(
                appUserStakeholder.StakeholderId,
                appUserStakeholder.AppUserId,
                appUserStakeholder.AppUser.Email ?? string.Empty,
                appUserStakeholder.Stakeholder.TenantId,
                appUserStakeholder.Stakeholder.CountryId,
                appUserStakeholder.Stakeholder.StakeholderTypeId,
                appUserStakeholder.Stakeholder.FirstName,
                appUserStakeholder.Stakeholder.LastName,
                appUserStakeholder.Stakeholder.AvatarUrl,
                appUserStakeholder.Stakeholder.IsVerified);
    }
}
