using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithUnknownEmail_ShouldReturnInvalidCredentialsAndPublishFailedEvent
{
    [Fact]
    public async Task Verify()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();

        var context = new AuthenticationFlowTestContext();
        context.IdentityService.FindByEmailAsync(email).Returns((BackendProjectTemplate.Domain.Authentication.Entities.AppUser?)null);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInCommand(
                email,
                password,
                ipAddress,
                userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.InvalidCredentials);
        result.AccessToken.ShouldBeNull();
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInFailed>(message =>
                message.EmailAddress == email &&
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.FailureReason == "user_not_found"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
