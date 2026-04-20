using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Authentication.Stakeholders;

public sealed class StakeholderResolver(IRepository<Stakeholder> stakeholderRepository)
{
    public async Task<Stakeholder> GetRequiredAsync(Guid appUserId, CancellationToken cancellationToken)
    {
        var stakeholder = await stakeholderRepository.FirstOrDefaultAsync(
            new StakeholderByAppUserIdSpecification(appUserId),
            cancellationToken);
        if (stakeholder is null)
        {
            throw new InvalidOperationException($"Unable to resolve stakeholder for user '{appUserId}'.");
        }

        return stakeholder;
    }

    public async Task<Guid> GetRequiredIdAsync(Guid appUserId, CancellationToken cancellationToken) =>
        (await GetRequiredAsync(appUserId, cancellationToken)).Id;
}
