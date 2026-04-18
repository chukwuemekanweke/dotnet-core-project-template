using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

public sealed class GoogleSignInHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IAccessTokenService accessTokenService,
    IEventPublisher eventPublisher,
    IRepository<AppUserStakeholder> appUserStakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignInResult> HandleAsync(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        var googleIdentity = await googleIdentityTokenService.ValidateAsync(request.IdToken, cancellationToken);
        if (googleIdentity is null)
        {
            return new GoogleSignInResult(GoogleSignInStatus.InvalidGoogleToken, null);
        }

        var user = await identityService.FindByLoginAsync(ExternalLoginProviders.Google, googleIdentity.Subject);
        if (user is null)
        {
            await PublishFailedAsync(
                stakeholderId: null,
                emailAddress: googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.UserNotFound,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.AccountNotRegistered, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            var stakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);

            await PublishFailedAsync(
                stakeholderId: stakeholder.StakeholderId,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            var stakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.StakeholderId,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.EmailNotVerified, null);
        }

        var currentStakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);
        var accessToken = accessTokenService.Generate(user, currentStakeholder.StakeholderId);

        await PublishSuccessfulAsync(
            stakeholderId: currentStakeholder.StakeholderId,
            emailAddress: user.Email ?? googleIdentity.Email,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoogleSignInResult(GoogleSignInStatus.Success, accessToken);
    }

    private async Task PublishSuccessfulAsync(
        Guid stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(ipAddress, userAgent)
        {
            StakeholderId = stakeholderId,
            OccuredAt = now
        }, cancellationToken);

        var properties = new Dictionary<string, string>
        {
            [Observability.StakeholderIdPropertyName] = stakeholderId.ToString()
        };
        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInSuccessful, properties);
    }

    private async Task PublishFailedAsync(
        Guid? stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInFailed(emailAddress, ipAddress, userAgent, failureReason)
        {
            StakeholderId = stakeholderId,
            OccuredAt = now
        }, cancellationToken);

        var properties = new Dictionary<string, string>
        {
            ["FailureReason"] = failureReason
        };

        if (stakeholderId.HasValue)
        {
            properties[Observability.StakeholderIdPropertyName] = stakeholderId.Value.ToString();
        }

        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInFailed, properties);
    }

    private async Task<AppUserStakeholder> GetRequiredStakeholderAsync(Guid userId, CancellationToken cancellationToken)
    {
        var appUserStakeholder = await appUserStakeholderRepository.FirstOrDefaultAsync(
            new AppUserStakeholderByAppUserIdSpecification(userId),
            cancellationToken);
        if (appUserStakeholder is null)
        {
            throw new InvalidOperationException($"Unable to resolve stakeholder for user '{userId}'.");
        }

        return appUserStakeholder;
    }
}
