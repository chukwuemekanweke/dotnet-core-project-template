using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Domain.Authentication.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.GoogleSignUp;

public sealed class WhenSigningUpWithGoogleIdentityAndMismatchedIpCountry_Should
{
    [Fact]
    public async Task RejectRequest()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignUpCommand();
        context.IpGeolocationService.GetGeolocationAsync(command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new IpGeolocation("Paris", "Ile-de-France", "France"));

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.CountryMismatch);
        await context.GoogleIdentityTokenService.DidNotReceive().ValidateAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
