using System.Net;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_SigningIn_WithValidCredentials_Should
{
    [Fact]
    public async Task ReturnAccessToken()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignInRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignInRequest>>();
        var refreshValidator = Substitute.For<IValidator<RefreshSessionRequest>>();
        var request = new SignInRequest("jane@example.com", "P@ssw0rd123!");
        var user = context.CreateUser(request.Email);
        user.MarkEmailVerified(context.Clock.GetUtcNow());
        var stakeholder = context.CreateStakeholder(user.Id);

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, request.Password).Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id)
            .Returns(new AccessToken("access-token", context.Clock.GetUtcNow().AddMinutes(15)));
        context.RefreshTokenService.IssueAsync(user, Arg.Any<CancellationToken>())
            .Returns(new RefreshToken("refresh-token", context.Clock.GetUtcNow().AddDays(7)));

        var sut = new SessionsController(
            context.CreateSignInHandler(),
            context.CreateGoogleSignInHandler(),
            context.CreateLogoutSessionHandler(),
            context.CreateRefreshSessionHandler(),
            validator,
            googleValidator,
            refreshValidator)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        sut.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        sut.Request.Headers.UserAgent = "Unit Test";

        var result = await sut.Handle(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SignInResponse>();
        payload.AccessToken.ShouldBe("access-token");
        payload.RefreshToken.ShouldBe("refresh-token");
    }
}
