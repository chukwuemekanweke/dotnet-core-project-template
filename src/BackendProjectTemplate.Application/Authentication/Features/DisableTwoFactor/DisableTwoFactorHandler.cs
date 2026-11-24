using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.DisableTwoFactor;

public sealed class DisableTwoFactorHandler(
    TwoFactorActorResolver actorResolver,
    TwoFactorProofVerifier proofVerifier,
    IAuthenticationIdentityService identityService,
    IAuthenticationSessionService sessionService,
    ICustomTelemetryContext telemetryContext,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<DisableTwoFactorResult> HandleAsync(
        DisableTwoFactorCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return DisableTwoFactorResult.NotAuthenticated;
        if (!await identityService.GetTwoFactorEnabledAsync(actor.User))
            return DisableTwoFactorResult.NotEnabled;
        if (!await proofVerifier.VerifyAsync(actor.User, command.VerificationMethod, command.Code))
            return DisableTwoFactorResult.InvalidProof;

        if (!(await identityService.UpdateSecurityStampAsync(actor.User)).Succeeded)
            return DisableTwoFactorResult.Failed;
        if (!(await identityService.SetTwoFactorEnabledAsync(actor.User, false)).Succeeded)
            return DisableTwoFactorResult.Failed;
        if (!(await identityService.ResetAuthenticatorKeyAsync(actor.User)).Succeeded)
            return DisableTwoFactorResult.Failed;
        await sessionService.RevokeOthersAsync(command.CurrentSessionId, actor.StakeholderId, cancellationToken);
        await eventPublisher.PublishAsync(
            new UserSecurityActivityOccurred(UserSecurityActivityTypes.TwoFactorDisabled, command.IpAddress, command.UserAgent)
            {
                StakeholderId = actor.StakeholderId,
                FlowId = command.ActorContext.FlowId,
                OccuredAt = timeProvider.GetUtcNow()
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorDisabled,
            ObservabilityEventProperties.Create(command.ActorContext, actor.StakeholderId));
        return DisableTwoFactorResult.Success;
    }
}
