using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed class SignInHandler(
    IAuthenticationIdentityService identityService,
    IAccessTokenService accessTokenService,
    IEventPublisher eventPublisher,
    IRepository<AppUserStakeholder> appUserStakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignInResult> HandleAsync(SignInCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email);

        if (user is null)
        {
            await PublishFailedAsync(
                stakeholderId: null,
                emailAddress: request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.UserNotFound,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            var stakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);

            await PublishFailedAsync(
                stakeholderId: stakeholder.StakeholderId,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            var stakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.StakeholderId,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.EmailNotVerified, null);
        }

        if (!await identityService.CheckPasswordAsync(user, request.Password))
        {
            var stakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.StakeholderId,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.InvalidCredentials,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        var currentStakeholder = await GetRequiredStakeholderAsync(user.Id, cancellationToken);
        var accessToken = accessTokenService.Generate(user, currentStakeholder.StakeholderId);

        await PublishSuccessfulAsync(
            stakeholderId: currentStakeholder.StakeholderId,
            emailAddress: user.Email ?? request.Email,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SignInResult(SignInStatus.Success, accessToken);
    }

    private async Task PublishSuccessfulAsync(
        Guid stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(emailAddress, ipAddress, userAgent)
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
