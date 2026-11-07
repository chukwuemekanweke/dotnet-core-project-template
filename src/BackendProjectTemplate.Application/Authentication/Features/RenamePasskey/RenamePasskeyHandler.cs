using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.RenamePasskey;

public sealed class RenamePasskeyHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver)
{
    public async Task<PasskeyOperationStatus> HandleAsync(
        RenamePasskeyCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(command.AppUserId);
        if (user is null || user.IsDeleted)
            return PasskeyOperationStatus.NotFound;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        return stakeholder.Id == command.StakeholderId && stakeholder.TenantId == command.TenantId
            ? await passkeyService.RenameAsync(user, command.CredentialId, command.Name)
            : PasskeyOperationStatus.NotFound;
    }
}
