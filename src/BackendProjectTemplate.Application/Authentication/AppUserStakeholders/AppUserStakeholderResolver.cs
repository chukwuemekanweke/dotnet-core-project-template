using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Persistence;

namespace BackendProjectTemplate.Application.Authentication.AppUserStakeholders;

public sealed class AppUserStakeholderResolver(IAppUserStakeholderRepository appUserStakeholderRepository)
{
    public async Task<AppUserStakeholder> GetRequiredStakeholderAsync(Guid userId, CancellationToken cancellationToken)
    {
        var appUserStakeholder = await appUserStakeholderRepository.GetByAppUserIdAsync(userId, cancellationToken);
        if (appUserStakeholder is null)
        {
            throw new InvalidOperationException($"Unable to resolve stakeholder for user '{userId}'.");
        }

        return appUserStakeholder;
    }

    public async Task<Guid> GetRequiredStakeholderIdAsync(Guid userId, CancellationToken cancellationToken) =>
        (await GetRequiredStakeholderAsync(userId, cancellationToken)).StakeholderId;
}
