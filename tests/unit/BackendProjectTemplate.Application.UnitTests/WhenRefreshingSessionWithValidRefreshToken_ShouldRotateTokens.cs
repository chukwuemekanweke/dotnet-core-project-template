using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRefreshingSessionWithValidRefreshToken_ShouldRotateTokens
{
    [Fact]
    public async Task Verify()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var stakeholderId = Guid.CreateVersion7();
        var securityStamp = Guid.CreateVersion7().ToString("N");
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, now);
        user.MarkEmailVerified(now);
        user.SecurityStamp = securityStamp;

        var storedRefreshToken = AuthenticationRefreshToken.Create(
            user.Id,
            "HASH",
            securityStamp,
            now.AddDays(30),
            now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholderId, now);
        var expectedAccessToken = new AccessToken("access-token", now.AddMinutes(5));
        var expectedRefreshToken = new RefreshToken("refresh-token", now.AddDays(30));

        var context = new AuthenticationFlowTestContext();
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns(securityStamp);
        context.AppUserStakeholderRepository.GetByAppUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(appUserStakeholder);
        context.AccessTokenService.Generate(user, stakeholderId).Returns(expectedAccessToken);
        context.RefreshTokenService.RotateAsync(storedRefreshToken, user, Arg.Any<CancellationToken>())
            .Returns(expectedRefreshToken);

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand("refresh-token"),
            CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.Success);
        result.Tokens.ShouldNotBeNull();
        result.Tokens.AccessToken.ShouldBe(expectedAccessToken);
        result.Tokens.RefreshToken.ShouldBe(expectedRefreshToken);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
