using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Authentication.AppUserStakeholders;

public sealed class AppUserStakeholderResolver(IRepository<AppUserStakeholder> appUserStakeholderRepository)
{
    public async Task<AppUserStakeholder> GetRequiredStakeholderAsync(Guid userId, CancellationToken cancellationToken)
    {
        var appUserStakeholder = await appUserStakeholderRepository.FirstOrDefaultAsync(
            new AppUserStakeholderByAppUserIdSpecification(userId),
            cancellationToken);
        if (appUserStakeholder is null)
        {
            throw new InvalidOperationException($"Unable to resolve stakeholder for user '{userId}'.");
        }

        return appUserStakeholder;
    }

    public async Task<Guid> GetRequiredStakeholderIdAsync(Guid userId, CancellationToken cancellationToken) =>
        (await GetRequiredStakeholderAsync(userId, cancellationToken)).StakeholderId;
}
