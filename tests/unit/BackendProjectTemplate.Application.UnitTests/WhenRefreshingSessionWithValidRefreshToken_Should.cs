using BackendProjectTemplate.Application.Authentication.Features.RefreshSession;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRefreshingSessionWithValidRefreshToken_Should
{
    [Fact]
    public async Task RotateTokens()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
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
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), firstName, lastName, now);
        var expectedAccessToken = new AccessToken("access-token", now.AddMinutes(5));
        var expectedRefreshToken = new RefreshToken("refresh-token", now.AddDays(30));

        var context = new AuthenticationFlowTestContext();
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns(securityStamp);
        context.StakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id).Returns(expectedAccessToken);
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

