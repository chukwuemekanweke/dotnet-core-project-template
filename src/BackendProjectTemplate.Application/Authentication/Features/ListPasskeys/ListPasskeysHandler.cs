using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.ListPasskeys;

public sealed class ListPasskeysHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver)
{
    public async Task<IReadOnlyList<PasskeyCredential>?> HandleAsync(
        ListPasskeysQuery query,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(query.AppUserId);
        if (user is null || user.IsDeleted)
            return null;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        return stakeholder.Id == query.StakeholderId && stakeholder.TenantId == query.TenantId
            ? await passkeyService.ListAsync(user)
            : null;
    }
}
