using BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithRegisteredGoogleIdentity_ShouldReturnAccessToken
{
    [Fact]
    public async Task Verify()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();
        var stakeholderId = Guid.CreateVersion7();
        var subject = Guid.CreateVersion7().ToString("N");
        const string token = "signed-jwt";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, now);
        user.MarkEmailVerified(now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholderId, now);

        var context = new AuthenticationFlowTestContext();
        var expectedToken = new AccessToken(token, now.AddHours(1));
        var expectedRefreshToken = new RefreshToken("refresh-token", now.AddDays(30));

        context.GoogleIdentityTokenService.ValidateAsync("google-id-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByLoginAsync("Google", subject).Returns(user);
        context.AppUserStakeholderRepository.GetByAppUserIdAsync(
                user.Id,
                Arg.Any<CancellationToken>())
            .Returns(appUserStakeholder);
        context.AccessTokenService.Generate(user, stakeholderId).Returns(expectedToken);
        context.RefreshTokenService.IssueAsync(user, Arg.Any<CancellationToken>()).Returns(expectedRefreshToken);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateGoogleSignInCommand(
                idToken: "google-id-token",
                ipAddress: ipAddress,
                userAgent: userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(GoogleSignInStatus.Success);
        result.Tokens.ShouldNotBeNull();
        result.Tokens.AccessToken.ShouldBe(expectedToken);
        result.Tokens.RefreshToken.ShouldBe(expectedRefreshToken);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message =>
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.StakeholderId == stakeholderId),
            Arg.Any<CancellationToken>());
    }
}
