using BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithPasskeyAndTwoFactorEnabled_Should
{
    [Fact]
    public async Task ReturnChallengeWithoutCreatingSession()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var actor = new ActorContext(null, stakeholder.TenantId, "correlation", "flow");
        var command = new CompletePasskeySignInCommand(
            stakeholder.TenantId, "flow-token", "{}", "127.0.0.1", "browser", actor);
        var challenge = new TwoFactorChallenge("mfa", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Passkey, actor, command.IpAddress, command.UserAgent,
            context.Clock.GetUtcNow().AddMinutes(5), 5);
        context.PasskeyService.VerifyAuthenticationAsync(
                stakeholder.TenantId, command.FlowToken, command.CredentialJson, Arg.Any<CancellationToken>())
            .Returns(new PasskeyAuthenticationResult(PasskeyOperationStatus.Success, user, context.CreatePasskeyInfo()));
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.TwoFactorChallengeService.StartAsync(
                user.Id, stakeholder.Id, stakeholder.TenantId, AuthenticationMethod.Passkey,
                actor, command.IpAddress, command.UserAgent, Arg.Any<CancellationToken>())
            .Returns(challenge);

        var result = await context.CreateCompletePasskeySignInHandler()
            .HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(CompletePasskeySignInStatus.RequiresTwoFactor);
        result.Challenge.ShouldBe(challenge);
        result.Tokens.ShouldBeNull();
        await context.PasskeyService.Received(1)
            .UpdateAuthenticationStateAsync(user, Arg.Any<UserPasskeyInfo>());
        await context.SessionService.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default!, default, default);
        await context.EventPublisher.DidNotReceiveWithAnyArgs()
            .PublishAsync(default(UserSignInSuccessful)!, default);
    }
}
