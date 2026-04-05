using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningUpWithRejectedPassword_ShouldReturnValidationFailure
{
    [Fact]
    public async Task Verify()
    {
        const string email = "ada@example.com";
        const string password = "weakpass";
        const string firstName = "Ada";
        const string lastName = "Lovelace";

        var context = new AuthenticationFlowTestContext();

        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), password).Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
                Description = "Passwords must have at least one digit ('0'-'9')."
            }));

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpRequest(email, password, firstName, lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.ValidationFailed);
        result.ValidationErrors.ShouldNotBeNull();
        result.ValidationErrors.ShouldContainKey(nameof(SignUpRequest.Password));
        await context.OtpDeliveryService.DidNotReceive().SendSignUpOtpAsync(
            Arg.Any<AppUser>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await context.OutboxWriter.DidNotReceive().AddEventAsync(
            Arg.Any<UserCreated>(),
            Arg.Any<CancellationToken>());
    }
}
