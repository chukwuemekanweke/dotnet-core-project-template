using BackendProjectTemplate.Application.Authentication.AppUserStakeholders;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.RefreshSession;

public sealed class RefreshSessionHandler(
    IAuthenticationIdentityService identityService,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    AppUserStakeholderResolver appUserStakeholderResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RefreshSessionResult> HandleAsync(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var currentRefreshToken = await refreshTokenService.FindByTokenAsync(request.RefreshToken, cancellationToken);
        if (currentRefreshToken is null)
        {
            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var user = await identityService.FindByIdAsync(currentRefreshToken.AppUserId);
        if (user is null)
        {
            refreshTokenService.Revoke(currentRefreshToken, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var currentSecurityStamp = await identityService.GetSecurityStampAsync(user);
        var utcNow = timeProvider.GetUtcNow();
        if (!currentRefreshToken.CanBeRedeemed(currentSecurityStamp, utcNow))
        {
            refreshTokenService.Revoke(currentRefreshToken, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            return new RefreshSessionResult(RefreshSessionStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            return new RefreshSessionResult(RefreshSessionStatus.EmailNotVerified, null);
        }

        var currentStakeholder = await appUserStakeholderResolver.GetRequiredStakeholderAsync(user.Id, cancellationToken);
        var accessToken = accessTokenService.Generate(user, currentStakeholder.StakeholderId);
        var refreshToken = await refreshTokenService.RotateAsync(currentRefreshToken, user, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshSessionResult(
            RefreshSessionStatus.Success,
            new AuthenticationTokens(accessToken, refreshToken));
    }
}
