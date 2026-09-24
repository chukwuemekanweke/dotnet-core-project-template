using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Authentication;

public sealed class AuthenticationSessionIssuer(
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthenticationSessionService sessionService)
{
    public async Task<AuthenticationTokens> IssueAsync(
        AppUser user,
        Stakeholder stakeholder,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var session = await sessionService.CreateAsync(
            stakeholder,
            ipAddress,
            userAgent,
            refreshTokenService.GetExpiry(),
            cancellationToken);
        var accessToken = accessTokenService.Generate(user, stakeholder.Id, session.Id);
        var refreshToken = await refreshTokenService.IssueAsync(
            user,
            session.Id,
            session.ExpiresAtUtc,
            cancellationToken);

        return new AuthenticationTokens(accessToken, refreshToken);
    }
}
