using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithTwoFactorEnabled_Should
{
    [Fact]
    public async Task ReturnChallengeWithoutCreatingSession()
    {
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateSignInCommand(email: user.Email);
        var challenge = new TwoFactorChallenge("opaque", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Password, command.ActorContext, command.IpAddress, command.UserAgent,
            context.Clock.GetUtcNow().AddMinutes(5), 5);
        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, command.Password).Returns(true);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.TwoFactorChallengeService.StartAsync(user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Password, command.ActorContext, command.IpAddress, command.UserAgent,
            Arg.Any<CancellationToken>()).Returns(challenge);

        var result = await context.CreateSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.RequiresTwoFactor);
        result.Tokens.ShouldBeNull();
        result.Challenge.ShouldBe(challenge);
        await context.SessionService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default);
        await context.EventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default(UserSignInSuccessful)!, default);
    }
}
