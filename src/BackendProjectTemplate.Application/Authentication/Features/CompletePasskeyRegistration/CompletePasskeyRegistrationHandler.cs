using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeyRegistration;

public sealed class CompletePasskeyRegistrationHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext telemetryContext,
    ICurrentActor currentActor)
{
    public async Task<PasskeyRegistrationResult> HandleAsync(
        CompletePasskeyRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(command.AppUserId);
        if (user is null || user.IsDeleted || !user.EmailConfirmed || await identityService.IsLockedOutAsync(user))
            return new PasskeyRegistrationResult(PasskeyOperationStatus.InvalidFlow);
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.Id != command.StakeholderId || stakeholder.TenantId != command.TenantId)
            return new PasskeyRegistrationResult(PasskeyOperationStatus.InvalidFlow);

        var result = await passkeyService.CompleteRegistrationAsync(
            user, stakeholder.Id, stakeholder.TenantId, command.FlowToken, command.CredentialJson, cancellationToken);
        if (result.Status == PasskeyOperationStatus.Success)
        {
            telemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasskeyRegistrationCompleted,
                ObservabilityEventProperties.Create(ActorContext.FromCurrentActor(currentActor), stakeholder.Id));
        }
        return result;
    }
}
