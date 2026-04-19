using BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenCompletingPasswordResetWithValidOtp_ShouldResetPassword
{
    [Fact]
    public async Task Verify()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var otp = AuthenticationTestData.Otp();
        var password = AuthenticationTestData.StrongPassword();
        const string resetToken = "reset-token";

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(user.Id, otp, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(true);
        context.IdentityService.GeneratePasswordResetTokenAsync(user).Returns(resetToken);
        context.IdentityService.ResetPasswordAsync(user, resetToken, password).Returns(IdentityResult.Success);

        var result = await context.CreateCompletePasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateCompletePasswordResetCommand(
                email: user.Email,
                otp: otp,
                password: password,
                confirmPassword: password),
            CancellationToken.None);

        result.Status.ShouldBe(CompletePasswordResetStatus.Success);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
