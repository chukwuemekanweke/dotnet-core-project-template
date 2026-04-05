using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenVerifyingOtpWithValidCode_ShouldMarkUserAsVerified
{
    [Fact]
    public async Task Verify()
    {
        const string email = "grace@example.com";
        const string firstName = "Grace";
        const string lastName = "Hopper";
        const string otp = "123456";

        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser(email, firstName, lastName);

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.VerifySignUpOtpAsync(user, otp).Returns(true);
        context.IdentityService.UpdateAsync(Arg.Is<AppUser>(candidate => candidate.EmailConfirmed)).Returns(IdentityResult.Success);

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpRequest(email, otp),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.Success);
        user.EmailConfirmed.ShouldBeTrue();
        user.UpdatedAtUtc.ShouldBe(context.Clock.GetUtcNow());
    }
}
