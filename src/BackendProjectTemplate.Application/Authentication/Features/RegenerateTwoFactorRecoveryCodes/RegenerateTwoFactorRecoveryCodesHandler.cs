using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed class RegenerateTwoFactorRecoveryCodesHandler(
    TwoFactorActorResolver actorResolver,
    TwoFactorProofVerifier proofVerifier,
    IAuthenticationIdentityService identityService,
    IAuthenticationSessionService sessionService,
    IOptions<TwoFactorAuthenticationOptions> options,
    ICustomTelemetryContext telemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RegenerateTwoFactorRecoveryCodesResult> HandleAsync(
        RegenerateTwoFactorRecoveryCodesCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.NotAuthenticated);
        if (!await identityService.GetTwoFactorEnabledAsync(actor.User))
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.NotEnabled);
        if (!await proofVerifier.VerifyAsync(actor.User, command.VerificationMethod, command.Code))
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.InvalidProof);

        var codes = (await identityService.GenerateNewTwoFactorRecoveryCodesAsync(
            actor.User,
            options.Value.RecoveryCodeCount))?.ToArray();
        if (codes is null || codes.Length == 0)
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.Failed);

        var stamp = await identityService.UpdateSecurityStampAsync(actor.User);
        if (!stamp.Succeeded)
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.Failed);
        await sessionService.RevokeAllAsync(actor.StakeholderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorRecoveryCodesRegenerated,
            ObservabilityEventProperties.Create(command.ActorContext, actor.StakeholderId));
        return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.Success, codes);
    }
}
