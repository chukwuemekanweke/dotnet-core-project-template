using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithConfirmedUser_ShouldReturnAccessToken
{
    [Fact]
    public async Task Verify()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();
        const string token = "signed-jwt";
        var stakeholderId = Guid.CreateVersion7();

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, now);
        user.MarkEmailVerified(now);
        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholderId, now);

        var context = new AuthenticationFlowTestContext();
        var expectedToken = new AccessToken(token, now.AddHours(1));

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, password).Returns(true);
        context.AppUserStakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<AppUserStakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(appUserStakeholder);
        context.AccessTokenService.Generate(user, stakeholderId).Returns(expectedToken);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInCommand(
                email,
                password,
                ipAddress,
                userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.Success);
        result.AccessToken.ShouldBe(expectedToken);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message =>
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.StakeholderId == stakeholderId),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
