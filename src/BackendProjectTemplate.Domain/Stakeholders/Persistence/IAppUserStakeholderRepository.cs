using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Persistence;

public interface IAppUserStakeholderRepository
{
    Task<AppUserStakeholder?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken);
    Task<AppUserStakeholder?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AppUserStakeholder>> ListByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AppUserStakeholder>> ListByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken);
    Task AddAsync(AppUserStakeholder appUserStakeholder, CancellationToken cancellationToken);
    void Remove(AppUserStakeholder appUserStakeholder);
}
