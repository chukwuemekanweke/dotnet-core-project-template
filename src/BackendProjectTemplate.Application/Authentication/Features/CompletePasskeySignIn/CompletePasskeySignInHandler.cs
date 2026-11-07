using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;

public sealed class CompletePasskeySignInHandler(
    IPasskeyService passkeyService,
    IAuthenticationIdentityService identityService,
    AuthenticationSessionIssuer sessionIssuer,
    StakeholderResolver stakeholderResolver,
    ITwoFactorChallengeService twoFactorChallengeService,
    IEventPublisher eventPublisher,
    ICustomTelemetryContext telemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CompletePasskeySignInResult> HandleAsync(
        CompletePasskeySignInCommand command,
        CancellationToken cancellationToken)
    {
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasskeySignInStarted,
            ObservabilityEventProperties.Create(command.ActorContext));
        var assertion = await passkeyService.CompleteAuthenticationAsync(
            command.TenantId, command.FlowToken, command.CredentialJson, cancellationToken);
        if (assertion.Status != PasskeyOperationStatus.Success || assertion.User is null)
        {
            telemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasskeySignInFailed,
                ObservabilityEventProperties.Create(command.ActorContext));
            return new CompletePasskeySignInResult(Map(assertion.Status));
        }

        var user = assertion.User;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.TenantId != command.TenantId)
            return new CompletePasskeySignInResult(CompletePasskeySignInStatus.TenantMismatch);
        if (user.IsDeleted)
            return new CompletePasskeySignInResult(CompletePasskeySignInStatus.InvalidCredential);
        if (await identityService.IsLockedOutAsync(user))
        {
            await PublishFailedAsync(user, stakeholder.Id, command, UserSignInFailureReasons.LockedOut, cancellationToken);
            return new CompletePasskeySignInResult(
                CompletePasskeySignInStatus.AccountLocked,
                LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(user));
        }
        if (!user.EmailConfirmed)
        {
            await PublishFailedAsync(user, stakeholder.Id, command, UserSignInFailureReasons.EmailNotVerified, cancellationToken);
            return new CompletePasskeySignInResult(CompletePasskeySignInStatus.EmailNotVerified);
        }
        if (await identityService.GetTwoFactorEnabledAsync(user))
        {
            var challenge = await twoFactorChallengeService.StartAsync(
                user.Id, stakeholder.Id, stakeholder.TenantId, AuthenticationMethod.Passkey,
                command.ActorContext, command.IpAddress, command.UserAgent, cancellationToken);
            telemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.TwoFactorChallengeRequired,
                ObservabilityEventProperties.Create(command.ActorContext, stakeholder.Id));
            return new CompletePasskeySignInResult(CompletePasskeySignInStatus.RequiresTwoFactor, Challenge: challenge);
        }

        var tokens = await sessionIssuer.IssueAsync(
            user, stakeholder, command.IpAddress, command.UserAgent, cancellationToken);
        await eventPublisher.PublishAsync(new UserSignInSuccessful(command.IpAddress, command.UserAgent)
        {
            StakeholderId = stakeholder.Id,
            FlowId = command.ActorContext.FlowId,
            OccuredAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasskeySignInCompleted,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholder.Id));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompletePasskeySignInResult(CompletePasskeySignInStatus.Success, tokens);
    }

    private async Task PublishFailedAsync(
        Domain.Authentication.Entities.AppUser user,
        Guid stakeholderId,
        CompletePasskeySignInCommand command,
        string reason,
        CancellationToken cancellationToken)
    {
        await eventPublisher.PublishAsync(new UserSignInFailed(
            user.Email ?? string.Empty, command.IpAddress, command.UserAgent, reason)
        {
            StakeholderId = stakeholderId,
            FlowId = command.ActorContext.FlowId,
            OccuredAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasskeySignInFailed,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CompletePasskeySignInStatus Map(PasskeyOperationStatus status) => status switch
    {
        PasskeyOperationStatus.ExpiredFlow => CompletePasskeySignInStatus.ExpiredFlow,
        PasskeyOperationStatus.ConsumedFlow => CompletePasskeySignInStatus.ConsumedFlow,
        PasskeyOperationStatus.InvalidCredential => CompletePasskeySignInStatus.InvalidCredential,
        _ => CompletePasskeySignInStatus.InvalidFlow
    };
}
