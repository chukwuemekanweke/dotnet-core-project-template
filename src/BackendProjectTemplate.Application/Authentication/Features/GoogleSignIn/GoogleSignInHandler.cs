using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

public sealed class GoogleSignInHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IGoogleAuthenticationFlowService googleAuthenticationFlowService,
    AuthenticationSessionIssuer sessionIssuer,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignInResult> HandleAsync(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignInStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var flowResult = await googleAuthenticationFlowService.TakeAsync(request.FlowToken, cancellationToken);
        if (flowResult.Status != GoogleAuthenticationFlowStatus.Success)
        {
            return new GoogleSignInResult(MapFlowStatus(flowResult.Status), null);
        }

        var flow = flowResult.Flow!;
        if (flow.State != GoogleAuthenticationFlowState.Initiated
            || (flow.TenantId != Guid.Empty && request.ActorContext.TenantId != flow.TenantId))
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new GoogleSignInResult(GoogleSignInStatus.GoogleFlowInvalid, null);
        }

        var googleIdentity = await googleIdentityTokenService.ValidateAsync(
            request.IdToken,
            flow.Nonce,
            cancellationToken);
        if (googleIdentity is null)
        {
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidGoogleToken);
            return new GoogleSignInResult(GoogleSignInStatus.InvalidGoogleCredential, null);
        }

        var user = await identityService.FindByLoginAsync(ExternalLoginProviders.Google, googleIdentity.Subject);
        if (user is null)
        {
            var continuationState = await identityService.FindByEmailAsync(googleIdentity.Email) is null
                ? GoogleAuthenticationFlowState.RegistrationRequired
                : GoogleAuthenticationFlowState.LinkRequired;
            var continuation = flow with
            {
                State = continuationState,
                Subject = googleIdentity.Subject,
                Email = googleIdentity.Email,
                EmailVerified = googleIdentity.EmailVerified,
                HostedDomain = googleIdentity.HostedDomain
            };
            await googleAuthenticationFlowService.RestoreAsync(continuation, cancellationToken);

            return new GoogleSignInResult(
                continuationState == GoogleAuthenticationFlowState.LinkRequired
                    ? GoogleSignInStatus.LinkRequired
                    : GoogleSignInStatus.RegistrationRequired,
                null);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (flow.TenantId != Guid.Empty && stakeholder.TenantId != flow.TenantId)
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new GoogleSignInResult(GoogleSignInStatus.GoogleFlowInvalid, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);

            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new GoogleSignInResult(GoogleSignInStatus.EmailVerificationRequired, null);
        }

        var tokens = await sessionIssuer.IssueAsync(
            user,
            stakeholder,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await PublishSuccessfulAsync(
            stakeholderId: stakeholder.Id,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            request.ActorContext,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);

        return new GoogleSignInResult(GoogleSignInStatus.Success, tokens);
    }

    private static GoogleSignInStatus MapFlowStatus(GoogleAuthenticationFlowStatus status) => status switch
    {
        GoogleAuthenticationFlowStatus.Expired => GoogleSignInStatus.GoogleFlowExpired,
        GoogleAuthenticationFlowStatus.Consumed => GoogleSignInStatus.GoogleFlowConsumed,
        _ => GoogleSignInStatus.GoogleFlowInvalid
    };

    private async Task PublishSuccessfulAsync(
        Guid stakeholderId,
        string ipAddress,
        string userAgent,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(ipAddress, userAgent)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignInCompleted,
            ObservabilityEventProperties.Create(actorContext, stakeholderId));
    }

    private async Task PublishFailedAsync(
        Guid? stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        string failureReason,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInFailed(emailAddress, ipAddress, userAgent, failureReason)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        if (stakeholderId.HasValue)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
        }

        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, failureReason);
    }
}
