using System.Linq.Expressions;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class StakeholderReadModelRepository(AppReadDbContext dbContext) : IStakeholderReadModelRepository
{
    public async Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        await dbContext.AppUserStakeholders
            .Where(appUserStakeholder => appUserStakeholder.AppUserId == appUserId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default)
        => await dbContext.AppUserStakeholders
            .Where(appUserStakeholder => appUserStakeholder.StakeholderId == stakeholderId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    private static Expression<Func<AppUserStakeholder, StakeholderReadModel>> CreateReadModel() =>
        appUserStakeholder => new StakeholderReadModel(
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
