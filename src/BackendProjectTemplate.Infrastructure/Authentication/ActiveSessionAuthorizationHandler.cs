using System.IdentityModel.Tokens.Jwt;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class ActiveSessionAuthorizationHandler(IAccessTokenRevocationService accessTokenRevocationService)
    : AuthorizationHandler<ActiveSessionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSessionRequirement requirement)
    {
        var tokenId = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return;
        }

        if (await accessTokenRevocationService.IsRevokedAsync(tokenId, CancellationToken.None))
        {
            return;
        }

        context.Succeed(requirement);
    }
}
