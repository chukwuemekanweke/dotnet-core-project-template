using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningUpWithNewEmail_ShouldCreateIdentityUserAndPublishUserCreatedEvent
{
    [Fact]
    public async Task Verify()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), password).Returns(IdentityResult.Success);

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.FirstName == firstName &&
                user.LastName == lastName),
            password);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message =>
                message.EmailAddress == email),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
