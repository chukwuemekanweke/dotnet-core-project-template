using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AppUserStakeholderRepository(AppDbContext dbContext) : IAppUserStakeholderRepository
{
    public Task<AppUserStakeholder?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken) =>
        CreateTrackedQuery()
            .FirstOrDefaultAsync(appUserStakeholder => appUserStakeholder.AppUserId == appUserId, cancellationToken);

    public Task<AppUserStakeholder?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken) =>
        CreateTrackedQuery()
            .FirstOrDefaultAsync(appUserStakeholder => appUserStakeholder.StakeholderId == stakeholderId, cancellationToken);

    public async Task<IReadOnlyList<AppUserStakeholder>> ListByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken) =>
        await dbContext.AppUserStakeholders
            .Where(appUserStakeholder => appUserStakeholder.AppUserId == appUserId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AppUserStakeholder>> ListByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken) =>
        await dbContext.AppUserStakeholders
            .Where(appUserStakeholder => appUserStakeholder.StakeholderId == stakeholderId)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AppUserStakeholder appUserStakeholder, CancellationToken cancellationToken) =>
        dbContext.AppUserStakeholders.AddAsync(appUserStakeholder, cancellationToken).AsTask();

    public void Remove(AppUserStakeholder appUserStakeholder) =>
        dbContext.AppUserStakeholders.Remove(appUserStakeholder);

    private IQueryable<AppUserStakeholder> CreateTrackedQuery() =>
        dbContext.AppUserStakeholders
            .Include(appUserStakeholder => appUserStakeholder.AppUser)
            .Include(appUserStakeholder => appUserStakeholder.Stakeholder);
}
