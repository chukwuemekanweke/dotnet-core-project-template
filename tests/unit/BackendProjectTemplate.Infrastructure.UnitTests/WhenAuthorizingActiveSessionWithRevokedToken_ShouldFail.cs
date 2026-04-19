using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenAuthorizingActiveSessionWithRevokedToken_ShouldFail
{
    [Fact]
    public async Task Verify()
    {
        var tokenId = Guid.CreateVersion7().ToString("N");
        var revocationService = Substitute.For<IAccessTokenRevocationService>();
        revocationService.IsRevokedAsync(tokenId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = new ActiveSessionAuthorizationHandler(revocationService);
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, tokenId)
            ],
            authenticationType: "Bearer"));
        var context = new AuthorizationHandlerContext(
        [
            new ActiveSessionRequirement()
        ],
            user,
            resource: null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }
}
