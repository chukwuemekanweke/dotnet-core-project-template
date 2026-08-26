using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SignUp;

public sealed class WhenSigningUpWithMismatchedIpCountry_Should
{
    [Fact]
    public async Task RejectRequest()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateSignUpCommand();
        context.IpGeolocationService.GetGeolocationAsync(command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new IpGeolocation("Paris", "Ile-de-France", "France"));

        var result = await context.CreateSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.CountryMismatch);
        await context.IdentityService.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }
}
