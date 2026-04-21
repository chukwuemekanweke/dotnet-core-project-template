using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRefreshingSessionAfterPasswordChange_Should
{
    [Fact]
    public async Task InvalidateRefreshToken()
    {
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(
            AuthenticationTestData.Email(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName(),
            now);
        user.MarkEmailVerified(now);
        user.SecurityStamp = "current-stamp";

        var storedRefreshToken = AuthenticationRefreshToken.Create(
            user.Id,
            "HASH",
            "old-stamp",
            now.AddDays(30),
            now);

        var context = new AuthenticationFlowTestContext();
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns("current-stamp");

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand("refresh-token"),
            CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.InvalidRefreshToken);
        result.Tokens.ShouldBeNull();
        context.RefreshTokenService.Received(1).Revoke(storedRefreshToken, Arg.Any<DateTimeOffset>());
    }
}

