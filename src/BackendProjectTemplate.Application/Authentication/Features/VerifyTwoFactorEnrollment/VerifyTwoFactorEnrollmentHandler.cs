using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed class VerifyTwoFactorEnrollmentHandler(
    TwoFactorActorResolver actorResolver,
    IAuthenticationIdentityService identityService,
    IOptions<TwoFactorAuthenticationOptions> options,
    ICustomTelemetryContext telemetryContext,
    IAuthenticationSessionService sessionService,
    IUnitOfWork unitOfWork)
{
    public async Task<VerifyTwoFactorEnrollmentResult> HandleAsync(
        VerifyTwoFactorEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.NotAuthenticated);
        if (await identityService.GetTwoFactorEnabledAsync(actor.User))
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.AlreadyEnabled);
        if (!await identityService.VerifyAuthenticatorTokenAsync(actor.User, command.Code))
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.InvalidCode);

        var enabled = await identityService.SetTwoFactorEnabledAsync(actor.User, true);
        if (!enabled.Succeeded)
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.Failed);
        var codes = (await identityService.GenerateNewTwoFactorRecoveryCodesAsync(
            actor.User,
            options.Value.RecoveryCodeCount))?.ToArray();
        if (codes is null || codes.Length == 0)
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.Failed);
        if (!(await identityService.UpdateSecurityStampAsync(actor.User)).Succeeded)
            return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.Failed);

        await sessionService.RevokeAllAsync(actor.StakeholderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorEnrollmentCompleted,
            ObservabilityEventProperties.Create(command.ActorContext, actor.StakeholderId));
        return new VerifyTwoFactorEnrollmentResult(VerifyTwoFactorEnrollmentStatus.Success, codes);
    }
}
