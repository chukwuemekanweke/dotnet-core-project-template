using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class StakeholderReadModelRepository(AppDbContext dbContext) : IStakeholderReadModelRepository
{
    public Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        dbContext.AppUserStakeholders
            .Where(appUserStakeholder => appUserStakeholder.AppUserId == appUserId)
            .Select(appUserStakeholder => new StakeholderReadModel(
                appUserStakeholder.StakeholderId,
                appUserStakeholder.AppUserId,
                appUserStakeholder.Stakeholder.TenantId,
                appUserStakeholder.Stakeholder.CountryId,
                appUserStakeholder.Stakeholder.StakeholderTypeId))
            .FirstOrDefaultAsync(cancellationToken);
}
