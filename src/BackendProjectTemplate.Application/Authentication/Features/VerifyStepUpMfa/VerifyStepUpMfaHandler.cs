using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;

public sealed class VerifyStepUpMfaHandler(
    TwoFactorActorResolver actorResolver,
    IAuthenticationIdentityService identityService,
    IStepUpMfaProofStore proofStore,
    IOptions<StepUpMfaOptions> options,
    ICustomTelemetryContext telemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<VerifyStepUpMfaResult> HandleAsync(
        VerifyStepUpMfaCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.NotAuthenticated);
        if (command.VerificationMethod != TwoFactorVerificationMethod.Authenticator)
            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.InvalidMethod);
        if (!StepUpMfaOperations.All.Contains(command.Operation))
            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.InvalidOperation);
        if (!await identityService.GetTwoFactorEnabledAsync(actor.User))
        {
            Track(Observability.EventNames.Authentication.StepUpMfaEnrollmentMissing, command, actor.StakeholderId);
            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.EnrollmentRequired);
        }
        if (await identityService.IsLockedOutAsync(actor.User))
        {
            return new VerifyStepUpMfaResult(
                VerifyStepUpMfaStatus.AccountLocked,
                LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(actor.User));
        }

        Track(Observability.EventNames.Authentication.StepUpMfaVerificationRequested, command, actor.StakeholderId);
        if (!await identityService.VerifyAuthenticatorTokenAsync(actor.User, command.Code))
        {
            await identityService.AccessFailedAsync(actor.User);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            Track(Observability.EventNames.Authentication.StepUpMfaVerificationFailed, command, actor.StakeholderId);
            if (await identityService.IsLockedOutAsync(actor.User))
            {
                return new VerifyStepUpMfaResult(
                    VerifyStepUpMfaStatus.AccountLocked,
                    LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(actor.User));
            }

            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.InvalidCode);
        }

        if (!await proofStore.TryClaimAuthenticatorCodeAsync(
                actor.User.Id,
                command.Code,
                options.Value.ReplayWindow,
                cancellationToken))
        {
            Track(Observability.EventNames.Authentication.StepUpMfaReplayRejected, command, actor.StakeholderId);
            return new VerifyStepUpMfaResult(VerifyStepUpMfaStatus.CodeReplayed);
        }

        await identityService.ResetAccessFailedCountAsync(actor.User);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var issued = await proofStore.IssueAsync(
            new StepUpMfaProof(
                actor.User.Id,
                actor.StakeholderId,
                command.SessionId,
                await identityService.GetSecurityStampAsync(actor.User),
                command.Operation,
                command.RequestFingerprint,
                now,
                now.Add(options.Value.ProofLifetime)),
            cancellationToken);

        Track(Observability.EventNames.Authentication.StepUpMfaVerificationCompleted, command, actor.StakeholderId);
        Track(Observability.EventNames.Authentication.StepUpMfaProofIssued, command, actor.StakeholderId);
        return new VerifyStepUpMfaResult(
            VerifyStepUpMfaStatus.Success,
            issued.AuthorizationToken,
            issued.ExpiresAtUtc);
    }

    private void Track(string eventName, VerifyStepUpMfaCommand command, Guid stakeholderId) =>
        telemetryContext.AddCustomEvent(
            eventName,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Authentication.SessionId] = command.SessionId.ToString(),
                    [Observability.PropertyNames.Authentication.StepUpOperation] = command.Operation
                }));
}
