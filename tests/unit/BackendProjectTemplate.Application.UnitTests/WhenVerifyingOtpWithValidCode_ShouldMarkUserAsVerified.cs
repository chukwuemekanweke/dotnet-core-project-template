using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
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
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var otp = AuthenticationTestData.Otp();

        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser(email, firstName, lastName);

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.VerifySignUpOtpAsync(user, otp).Returns(true);
        context.IdentityService.UpdateAsync(Arg.Is<AppUser>(candidate => candidate.EmailConfirmed)).Returns(IdentityResult.Success);

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpCommand(email, otp),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.Success);
        user.EmailConfirmed.ShouldBeTrue();
        user.UpdatedAtUtc.ShouldBe(context.Clock.GetUtcNow());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Any<UserEmailConfirmed>(),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
