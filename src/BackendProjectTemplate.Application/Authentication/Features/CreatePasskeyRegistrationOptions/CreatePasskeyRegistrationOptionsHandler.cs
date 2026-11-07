using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyRegistrationOptions;

public sealed class CreatePasskeyRegistrationOptionsHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver)
{
    public async Task<PasskeyOptions?> HandleAsync(
        CreatePasskeyRegistrationOptionsCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(command.AppUserId);
        if (user is null || user.IsDeleted || !user.EmailConfirmed || await identityService.IsLockedOutAsync(user))
            return null;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.Id != command.StakeholderId || stakeholder.TenantId != command.TenantId)
            return null;
        return await passkeyService.CreateRegistrationOptionsAsync(
            user, stakeholder.Id, stakeholder.TenantId, command.PasskeyName, cancellationToken);
    }
}
