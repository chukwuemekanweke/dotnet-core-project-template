using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_LoggingOut_WithoutBearerToken_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var context = new AuthenticationControllerTestContext();
        var sut = new SessionsController(
            context.CreateSignInHandler(),
            context.CreateGoogleSignInHandler(),
            context.CreateLogoutSessionHandler(),
            context.CreateRefreshSessionHandler(),
            Substitute.For<IValidator<SignInRequest>>(),
            Substitute.For<IValidator<GoogleSignInRequest>>(),
            Substitute.For<IValidator<RefreshSessionRequest>>(),
            context.Clock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.Logout(CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedResult>();
    }
}
