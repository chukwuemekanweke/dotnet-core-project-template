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
                userId: null,
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

            await PublishFailedAsync(
                userId: user.Id,
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
            await PublishFailedAsync(
                userId: user.Id,
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
            await PublishFailedAsync(
                userId: user.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.InvalidCredentials,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        var accessToken = accessTokenService.Generate(user);

        await PublishSuccessfulAsync(
            userId: user.Id,
            emailAddress: user.Email ?? request.Email,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SignInResult(SignInStatus.Success, accessToken);
    }

    private async Task PublishSuccessfulAsync(
        Guid userId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(userId, emailAddress, ipAddress, userAgent)
        {
            OccuredAt = now
        }, cancellationToken);

        var properties = await BuildStakeholderPropertiesAsync(userId, cancellationToken);
        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInSuccessful, properties);
    }

    private async Task PublishFailedAsync(
        Guid? userId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInFailed(userId, emailAddress, ipAddress, userAgent, failureReason)
        {
            OccuredAt = now
        }, cancellationToken);

        var properties = new Dictionary<string, string>
        {
            ["FailureReason"] = failureReason
        };

        if (userId.HasValue)
        {
            var stakeholderProperties = await BuildStakeholderPropertiesAsync(userId.Value, cancellationToken);
            foreach (var property in stakeholderProperties)
            {
                properties[property.Key] = property.Value;
            }
        }

        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInFailed, properties);
    }

    private async Task<Dictionary<string, string>> BuildStakeholderPropertiesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var appUserStakeholder = await appUserStakeholderRepository.FirstOrDefaultAsync(
            new AppUserStakeholderByAppUserIdSpecification(userId),
            cancellationToken);
        if (appUserStakeholder is null)
        {
            return [];
        }

        return new Dictionary<string, string>
        {
            [Observability.StakeholderIdPropertyName] = appUserStakeholder.StakeholderId.ToString()
        };
    }
}
