using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;

public sealed class RemovePasskeyHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext telemetryContext,
    ICurrentActor currentActor)
{
    public async Task<PasskeyOperationStatus> HandleAsync(
        RemovePasskeyCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(command.AppUserId);
        if (user is null || user.IsDeleted)
            return PasskeyOperationStatus.NotFound;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.Id != command.StakeholderId || stakeholder.TenantId != command.TenantId)
            return PasskeyOperationStatus.NotFound;
        var result = await passkeyService.RemoveAsync(user, command.CredentialId);
        if (result == PasskeyOperationStatus.Success)
        {
            telemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasskeyRemoved,
                ObservabilityEventProperties.Create(ActorContext.FromCurrentActor(currentActor), stakeholder.Id));
        }
        return result;
    }
}
