using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningUpWithNewEmail_ShouldCreateIdentityUserAndSendOtp
{
    [Fact]
    public async Task Verify()
    {
        var context = new AuthenticationFlowTestContext();
        const string email = "ada@example.com";
        const string password = "P@ssw0rd123!";
        const string firstName = "Ada";
        const string lastName = "Lovelace";
        const string otp = "123456";

        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), password).Returns(IdentityResult.Success);
        context.IdentityService.GenerateSignUpOtpAsync(Arg.Any<AppUser>()).Returns(otp);

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        result.OtpExpiresAtUtc.ShouldBe(context.Clock.GetUtcNow().AddMinutes(3));
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.FirstName == firstName &&
                user.LastName == lastName),
            password);
        await context.OtpDeliveryService.Received(1).SendSignUpOtpAsync(
            Arg.Is<AppUser>(user => user.Email == email),
            otp,
            Arg.Any<CancellationToken>());
    }
}
