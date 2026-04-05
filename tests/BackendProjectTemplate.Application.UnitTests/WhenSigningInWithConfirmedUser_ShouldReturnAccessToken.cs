using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithConfirmedUser_ShouldReturnAccessToken
{
    [Fact]
    public async Task Verify()
    {
        const string email = "linus@example.com";
        const string password = "P@ssw0rd123!";
        const string firstName = "Linus";
        const string lastName = "Torvalds";
        const string token = "signed-jwt";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, now);
        user.MarkEmailVerified(now);

        var context = new AuthenticationFlowTestContext();
        var expectedToken = new AccessToken(token, now.AddHours(1));

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, password).Returns(true);
        context.AccessTokenService.Generate(user).Returns(expectedToken);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInRequest(email, password),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.Success);
        result.AccessToken.ShouldBe(expectedToken);
    }
}
